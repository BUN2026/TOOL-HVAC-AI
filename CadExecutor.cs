using System;
using System.IO;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace HvacPalette
{
    public static class CadExecutor
    {
        private static Point3d? _lastEndPoint = null;
        private static Vector3d? _lastDirection = null;
        private static double _lastWidth = 0;
        private static double _lastHeight = 0;

        public static void EnsureLayer(Database db, string layerName, short colorIndex)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                if (!lt.Has(layerName))
                {
                    lt.UpgradeOpen();
                    LayerTableRecord ltr = new LayerTableRecord
                    {
                        Name = layerName,
                        Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex)
                    };
                    lt.Add(ltr);
                    tr.AddNewlyCreatedDBObject(ltr, true);
                }
                tr.Commit();
            }
        }

        public static void EnsureRegApp(Database db, string regAppName)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                RegAppTable rat = (RegAppTable)tr.GetObject(db.RegAppTableId, OpenMode.ForRead);
                if (!rat.Has(regAppName))
                {
                    rat.UpgradeOpen();
                    RegAppTableRecord ratr = new RegAppTableRecord { Name = regAppName };
                    rat.Add(ratr);
                    tr.AddNewlyCreatedDBObject(ratr, true);
                }
                tr.Commit();
            }
        }

        public static void SetCurrentLayer(string layerName, short colorIndex)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Database db = doc.Database;

            using (doc.LockDocument())
            {
                EnsureLayer(db, layerName, colorIndex);
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                    if (lt.Has(layerName))
                    {
                        db.Clayer = lt[layerName];
                    }
                    tr.Commit();
                }
            }
        }

        /// <summary>
        /// Vẽ tuyến ống gió liên tục, tự động nhận điểm cũ, tự động chèn Co/Giảm và gắn Text chuẩn
        /// </summary>
        public static void DrawContinuousDuct(double widthMm, double heightMm, string layerDuct, string layerText, short colorIndex, string sysPrefix, bool isMainDuct)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            Point3d startPt;
            PromptPointOptions ppo1 = new PromptPointOptions("\nChọn điểm đầu tuyến ống gió (nhấn Enter để tiếp tục từ điểm cũ): ");
            
            if (_lastEndPoint.HasValue)
            {
                ppo1.Keywords.Add("Continue");
                ppo1.Keywords.Add("New");
                ppo1.Keywords.Default = "Continue";
                ppo1.Message = string.Format("\nChọn điểm đầu [Tiếp tục từ điểm cũ ({0:F0},{1:F0}) / Enter để nối tiếp]: ", _lastEndPoint.Value.X, _lastEndPoint.Value.Y);
            }

            PromptPointResult ppr1 = ed.GetPoint(ppo1);
            if (ppr1.Status == PromptStatus.None && _lastEndPoint.HasValue)
            {
                startPt = _lastEndPoint.Value;
            }
            else if (ppr1.Status == PromptStatus.OK)
            {
                startPt = ppr1.Value;
            }
            else
            {
                return;
            }

            Point3d currentPt = startPt;

            while (true)
            {
                PromptPointOptions ppo2 = new PromptPointOptions("\nChọn điểm kế tiếp (Enter hoặc ESC để kết thúc): ")
                {
                    UseBasePoint = true,
                    BasePoint = currentPt
                };

                PromptPointResult ppr2 = ed.GetPoint(ppo2);
                if (ppr2.Status != PromptStatus.OK) break;

                Point3d nextPt = ppr2.Value;
                if (currentPt.DistanceTo(nextPt) < 10.0)
                {
                    ed.WriteMessage("\n[HVAC] Khoảng cách quá ngắn, bỏ qua.");
                    continue;
                }

                Vector3d dir = (nextPt - currentPt).GetNormal();

                using (doc.LockDocument())
                {
                    EnsureLayer(db, layerDuct, colorIndex);
                    EnsureLayer(db, layerText, colorIndex);

                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                        BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                        // 1. TỰ ĐỘNG CHÈN CO (ELBOW) NẾU ĐỔI HƯỚNG TỪ TUYẾN TRƯỚC ĐÓ
                        if (_lastDirection.HasValue && _lastEndPoint.HasValue && currentPt.DistanceTo(_lastEndPoint.Value) < 1.0)
                        {
                            double angleDeg = Math.Abs(_lastDirection.Value.GetAngleTo(dir) * 180.0 / Math.PI);
                            if (angleDeg > 20.0 && angleDeg < 160.0)
                            {
                                DrawElbowFitting(btr, tr, currentPt, _lastDirection.Value, dir, widthMm, layerDuct, colorIndex);
                            }
                        }

                        // 2. TỰ ĐỘNG CHÈN CÔN THU (REDUCER) NẾU THAY ĐỔI TIẾT DIỆN
                        if (_lastWidth > 0 && Math.Abs(_lastWidth - widthMm) > 1.0)
                        {
                            DrawTransitionFitting(btr, tr, currentPt, dir, _lastWidth, widthMm, layerDuct, colorIndex);
                        }

                        // 3. VẼ ĐOẠN ỐNG THẲNG
                        Vector3d normal = new Vector3d(-dir.Y, dir.X, 0).MultiplyBy((widthMm / 2.0));

                        Line centerLine = new Line(currentPt, nextPt)
                        {
                            Layer = layerDuct,
                            Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex)
                        };
                        btr.AppendEntity(centerLine);
                        tr.AddNewlyCreatedDBObject(centerLine, true);

                        Line leftLine = new Line(currentPt + normal, nextPt + normal)
                        {
                            Layer = layerDuct,
                            Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex)
                        };
                        btr.AppendEntity(leftLine);
                        tr.AddNewlyCreatedDBObject(leftLine, true);

                        Line rightLine = new Line(currentPt - normal, nextPt - normal)
                        {
                            Layer = layerDuct,
                            Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex)
                        };
                        btr.AppendEntity(rightLine);
                        tr.AddNewlyCreatedDBObject(rightLine, true);

                        // 4. GẮN NHÃN TEXT KÍCH THƯỚC CHUẨN
                        InsertSmartDuctText(btr, tr, currentPt, nextPt, dir, normal, widthMm, heightMm, layerText, sysPrefix, isMainDuct);

                        tr.Commit();
                    }
                }

                _lastDirection = dir;
                _lastWidth = widthMm;
                _lastHeight = heightMm;
                _lastEndPoint = nextPt;
                currentPt = nextPt;
            }

            ed.WriteMessage("\n[HVAC] Kết thúc lệnh vẽ ống. Điểm cuối đã được lưu cho lần vẽ kế tiếp.");
        }

        private static void InsertSmartDuctText(BlockTableRecord btr, Transaction tr, Point3d pt1, Point3d pt2, Vector3d dir, Vector3d normal, double widthMm, double heightMm, string layerText, string sysPrefix, bool isMainDuct)
        {
            string textString;
            if (isMainDuct && !string.IsNullOrEmpty(sysPrefix))
            {
                textString = string.Format("{0} {1:F0}x{2:F0}", sysPrefix, widthMm, heightMm);
            }
            else
            {
                textString = string.Format("{0:F0}x{1:F0}", widthMm, heightMm);
            }

            double textHeight = 150.0;
            Point3d midPt = new Point3d((pt1.X + pt2.X) / 2.0, (pt1.Y + pt2.Y) / 2.0, (pt1.Z + pt2.Z) / 2.0);

            double angle = Math.Atan2(dir.Y, dir.X);
            if (angle > Math.PI / 2.0 || angle < -Math.PI / 2.0)
            {
                angle += Math.PI;
            }

            Point3d textPos;
            if (widthMm < (textHeight * 1.6) || heightMm < (textHeight * 1.2))
            {
                double offsetDist = (widthMm / 2.0) + 75.0;
                textPos = midPt + normal.GetNormal().MultiplyBy(offsetDist);
            }
            else
            {
                textPos = midPt;
            }

            DBText dbText = new DBText
            {
                TextString = textString,
                Position = textPos,
                Height = textHeight,
                Rotation = angle,
                Layer = layerText,
                HorizontalMode = TextHorizontalMode.TextCenter,
                VerticalMode = TextVerticalMode.TextVerticalMid,
                AlignmentPoint = textPos
            };

            btr.AppendEntity(dbText);
            tr.AddNewlyCreatedDBObject(dbText, true);
        }

        private static void DrawElbowFitting(BlockTableRecord btr, Transaction tr, Point3d centerPt, Vector3d dirIn, Vector3d dirOut, double widthMm, string layerDuct, short colorIndex)
        {
            Vector3d normIn = new Vector3d(-dirIn.Y, dirIn.X, 0).MultiplyBy(widthMm / 2.0);
            Vector3d normOut = new Vector3d(-dirOut.Y, dirOut.X, 0).MultiplyBy(widthMm / 2.0);

            Line conn1 = new Line(centerPt + normIn, centerPt + normOut)
            {
                Layer = layerDuct,
                Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex)
            };
            Line conn2 = new Line(centerPt - normIn, centerPt - normOut)
            {
                Layer = layerDuct,
                Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex)
            };

            btr.AppendEntity(conn1);
            tr.AddNewlyCreatedDBObject(conn1, true);
            btr.AppendEntity(conn2);
            tr.AddNewlyCreatedDBObject(conn2, true);
        }

        private static void DrawTransitionFitting(BlockTableRecord btr, Transaction tr, Point3d pt, Vector3d dir, double w1, double w2, string layerDuct, short colorIndex)
        {
            Vector3d norm1 = new Vector3d(-dir.Y, dir.X, 0).MultiplyBy(w1 / 2.0);
            Vector3d norm2 = new Vector3d(-dir.Y, dir.X, 0).MultiplyBy(w2 / 2.0);

            Line transLine1 = new Line(pt + norm1, pt + norm2)
            {
                Layer = layerDuct,
                Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex)
            };
            Line transLine2 = new Line(pt - norm1, pt - norm2)
            {
                Layer = layerDuct,
                Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex)
            };

            btr.AppendEntity(transLine1);
            tr.AddNewlyCreatedDBObject(transLine1, true);
            btr.AppendEntity(transLine2);
            tr.AddNewlyCreatedDBObject(transLine2, true);
        }

        /// <summary>
        /// Chèn Miệng Gió Block DXF, cập nhật Attribute và gán XData lưu lượng (Q, Neck, Face) để phục vụ Auto-Connect
        /// </summary>
        public static void InsertAirTerminalWithFlow(string dxfFileName, double flowM3h, double flowLs, double neckDia, string faceSize, string targetLayer, short colorIndex, bool attachText)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            string fullPath = Path.Combine(HvacConfigRepository.FittingDir, dxfFileName);
            if (!File.Exists(fullPath))
            {
                fullPath = Path.Combine(HvacConfigRepository.EquipmentDir, dxfFileName);
            }

            if (!File.Exists(fullPath))
            {
                ed.WriteMessage(string.Format("\n[HVAC Lỗi] Không tìm thấy file miệng gió: {0}", fullPath));
                return;
            }

            PromptPointOptions ppo = new PromptPointOptions("\nChọn vị trí chèn miệng gió trên bản vẽ: ");
            PromptPointResult ppr = ed.GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK) return;

            Point3d insertPt = ppr.Value;

            using (doc.LockDocument())
            {
                EnsureLayer(db, targetLayer, colorIndex);
                EnsureRegApp(db, "HVAC_DIFFUSER");

                string blockName = Path.GetFileNameWithoutExtension(dxfFileName);

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    ObjectId blockId = ObjectId.Null;

                    if (!bt.Has(blockName))
                    {
                        using (Database extDb = new Database(false, true))
                        {
                            extDb.DxfIn(fullPath, null);
                            blockId = db.Insert(blockName, extDb, true);
                        }
                    }
                    else
                    {
                        blockId = bt[blockName];
                    }

                    if (!blockId.IsNull)
                    {
                        BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                        BlockReference br = new BlockReference(insertPt, blockId)
                        {
                            Layer = targetLayer
                        };
                        btr.AppendEntity(br);
                        tr.AddNewlyCreatedDBObject(br, true);

                        // 1. CẬP NHẬT ATTRIBUTES CỦA BLOCK (TAG "Duct")
                        BlockTableRecord bDef = (BlockTableRecord)tr.GetObject(blockId, OpenMode.ForRead);
                        if (bDef.HasAttributeDefinitions)
                        {
                            foreach (ObjectId attId in bDef)
                            {
                                AttributeDefinition attDef = tr.GetObject(attId, OpenMode.ForRead) as AttributeDefinition;
                                if (attDef != null && !attDef.Constant)
                                {
                                    AttributeReference attRef = new AttributeReference();
                                    attRef.SetAttributeFromBlock(attDef, br.BlockTransform);
                                    if (string.Equals(attDef.Tag, "Duct", StringComparison.OrdinalIgnoreCase))
                                    {
                                        attRef.TextString = string.Format("{0} - Ø{1:F0} (Q={2:F0}m3/h)", faceSize, neckDia, flowM3h);
                                    }
                                    br.AttributeCollection.AppendAttribute(attRef);
                                    tr.AddNewlyCreatedDBObject(attRef, true);
                                }
                            }
                        }

                        // 2. GÁN XDATA LƯU LƯỢNG ĐỂ PHỤC VỤ VẼ TỰ ĐỘNG (AUTO-CONNECT)
                        ResultBuffer rb = new ResultBuffer(
                            new TypedValue((int)DxfCode.ExtendedDataRegAppName, "HVAC_DIFFUSER"),
                            new TypedValue((int)DxfCode.ExtendedDataAsciiString, dxfFileName),
                            new TypedValue((int)DxfCode.ExtendedDataReal, flowM3h),
                            new TypedValue((int)DxfCode.ExtendedDataReal, flowLs),
                            new TypedValue((int)DxfCode.ExtendedDataReal, neckDia),
                            new TypedValue((int)DxfCode.ExtendedDataAsciiString, faceSize)
                        );
                        br.XData = rb;

                        // 3. GẮN NHÃN TEXT PHỤ TRỢ NẾU BẬT TÙY CHỌN
                        if (attachText)
                        {
                            Point3d textPt = new Point3d(insertPt.X + 350.0, insertPt.Y + 350.0, insertPt.Z);
                            DBText qText = new DBText
                            {
                                TextString = string.Format("Q={0:F0}m³/h (Ø{1:F0})", flowM3h, neckDia),
                                Position = textPt,
                                Height = 120.0,
                                Layer = targetLayer,
                                Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex)
                            };
                            btr.AppendEntity(qText);
                            tr.AddNewlyCreatedDBObject(qText, true);
                        }
                    }

                    tr.Commit();
                }
            }

            ed.WriteMessage(string.Format("\n[HVAC] Đã chèn miệng gió {0} (Q={1:F0}m3/h, Cổ Ø{2:F0}) và gán XData thành công.", dxfFileName, flowM3h, neckDia));
        }

        /// <summary>
        /// Auto Nối Tuyến: Click chọn Miệng gió và Tuyến ống chính -> Tự động dẫn tuyến nhánh + ống mềm vào cổ box
        /// </summary>
        public static void AutoConnectTerminalToMainDuct(string layerDuct, string layerFlex, short colorIndex)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            PromptEntityOptions peo1 = new PromptEntityOptions("\nChọn Miệng gió (Block) cần kết nối: ");
            peo1.SetRejectMessage("\nChỉ được chọn BlockReference miệng gió!");
            peo1.AddAllowedClass(typeof(BlockReference), true);
            PromptEntityResult per1 = ed.GetEntity(peo1);
            if (per1.Status != PromptStatus.OK) return;

            PromptEntityOptions peo2 = new PromptEntityOptions("\nChọn Tuyến ống chính (Line tim ống): ");
            peo2.SetRejectMessage("\nChỉ được chọn Line hoặc Polyline tim ống chính!");
            peo2.AddAllowedClass(typeof(Line), true);
            PromptEntityResult per2 = ed.GetEntity(peo2);
            if (per2.Status != PromptStatus.OK) return;

            using (doc.LockDocument())
            {
                EnsureLayer(db, layerDuct, colorIndex);
                EnsureLayer(db, layerFlex, colorIndex);

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockReference br = tr.GetObject(per1.ObjectId, OpenMode.ForRead) as BlockReference;
                    Line ductLine = tr.GetObject(per2.ObjectId, OpenMode.ForRead) as Line;

                    if (br == null || ductLine == null) return;

                    Point3d termPt = br.Position;

                    // Chiếu trực giao tâm miệng gió lên tim ống chính
                    Point3d lineStart = ductLine.StartPoint;
                    Point3d lineEnd = ductLine.EndPoint;
                    Vector3d lineDir = (lineEnd - lineStart).GetNormal();

                    Vector3d v = termPt - lineStart;
                    double projDist = v.DotProduct(lineDir);
                    Point3d tapPt = lineStart + lineDir.MultiplyBy(projDist);

                    // Điểm kết thúc ống tôn cứng (cách miệng gió 500mm để uốn ống mềm)
                    Vector3d branchDir = (termPt - tapPt).GetNormal();
                    double totalDist = tapPt.DistanceTo(termPt);
                    double rigidDist = Math.Max(100.0, totalDist - 500.0);
                    Point3d flexStartPt = tapPt + branchDir.MultiplyBy(rigidDist);

                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                    // Vẽ ống nhánh tôn từ tim ống chính ra
                    Line branchLine = new Line(tapPt, flexStartPt)
                    {
                        Layer = layerDuct,
                        Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex)
                    };
                    btr.AppendEntity(branchLine);
                    tr.AddNewlyCreatedDBObject(branchLine, true);

                    // Vẽ đoạn ống mềm (dạng spline/arc nối tâm)
                    Line flexLine = new Line(flexStartPt, termPt)
                    {
                        Layer = layerFlex,
                        Color = Color.FromColorIndex(ColorMethod.ByAci, 30) // Màu đặc trưng ống mềm
                    };
                    btr.AppendEntity(flexLine);
                    tr.AddNewlyCreatedDBObject(flexLine, true);

                    tr.Commit();
                }
            }

            ed.WriteMessage("\n[HVAC] Đã tự động nối nhánh từ ống chính vào miệng gió.");
        }

        /// <summary>
        /// Vẽ cặp ống gas song song theo công suất dàn lạnh / dàn nóng VRV
        /// </summary>
        public static void DrawGasPipePair(double liquidDia, double suctionDia, double spacingMm, string layerGas, short colorIndex)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            PromptPointOptions ppo1 = new PromptPointOptions("\nChọn điểm đầu tuyến ống Gas: ");
            PromptPointResult ppr1 = ed.GetPoint(ppo1);
            if (ppr1.Status != PromptStatus.OK) return;

            Point3d startPt = ppr1.Value;

            PromptPointOptions ppo2 = new PromptPointOptions("\nChọn điểm cuối tuyến ống Gas: ")
            {
                UseBasePoint = true,
                BasePoint = startPt
            };
            PromptPointResult ppr2 = ed.GetPoint(ppo2);
            if (ppr2.Status != PromptStatus.OK) return;

            Point3d endPt = ppr2.Value;
            Vector3d dir = (endPt - startPt).GetNormal();
            Vector3d norm = new Vector3d(-dir.Y, dir.X, 0).MultiplyBy(spacingMm / 2.0);

            using (doc.LockDocument())
            {
                EnsureLayer(db, layerGas, colorIndex);

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                    // Ống lỏng
                    Line liqLine = new Line(startPt + norm, endPt + norm)
                    {
                        Layer = layerGas,
                        Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex)
                    };
                    btr.AppendEntity(liqLine);
                    tr.AddNewlyCreatedDBObject(liqLine, true);

                    // Ống hơi
                    Line sucLine = new Line(startPt - norm, endPt - norm)
                    {
                        Layer = layerGas,
                        Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex)
                    };
                    btr.AppendEntity(sucLine);
                    tr.AddNewlyCreatedDBObject(sucLine, true);

                    // Gán Text ống gas
                    Point3d midPt = new Point3d((startPt.X + endPt.X) / 2.0, (startPt.Y + endPt.Y) / 2.0, 0);
                    DBText txtGas = new DBText
                    {
                        TextString = string.Format("L:Ø{0:F2} / G:Ø{1:F2}", liquidDia, suctionDia),
                        Position = midPt + norm.MultiplyBy(1.5),
                        Height = 120.0,
                        Layer = layerGas,
                        Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex)
                    };
                    btr.AppendEntity(txtGas);
                    tr.AddNewlyCreatedDBObject(txtGas, true);

                    tr.Commit();
                }
            }

            ed.WriteMessage("\n[HVAC] Đã vẽ cặp ống gas thành công.");
        }

        public static void InsertDxfBlock(string blockFileName, string fullPath, string targetLayer)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            if (!File.Exists(fullPath))
            {
                ed.WriteMessage(string.Format("\n[HVAC Lỗi] Không tìm thấy file: {0}", fullPath));
                return;
            }

            PromptPointOptions ppo = new PromptPointOptions("\nChọn vị trí chèn thiết bị/phụ kiện: ");
            PromptPointResult ppr = ed.GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK) return;

            Point3d insertPt = ppr.Value;

            using (doc.LockDocument())
            {
                Database db = doc.Database;
                string blockName = Path.GetFileNameWithoutExtension(blockFileName);

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    ObjectId blockId = ObjectId.Null;

                    if (!bt.Has(blockName))
                    {
                        using (Database extDb = new Database(false, true))
                        {
                            if (fullPath.EndsWith(".dxf", StringComparison.OrdinalIgnoreCase))
                            {
                                extDb.DxfIn(fullPath, null);
                            }
                            else
                            {
                                extDb.ReadDwgFile(fullPath, FileOpenMode.OpenForReadAndAllShare, true, "");
                            }
                            blockId = db.Insert(blockName, extDb, true);
                        }
                    }
                    else
                    {
                        blockId = bt[blockName];
                    }

                    if (!blockId.IsNull)
                    {
                        BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                        BlockReference br = new BlockReference(insertPt, blockId)
                        {
                            Layer = targetLayer
                        };
                        btr.AppendEntity(br);
                        tr.AddNewlyCreatedDBObject(br, true);
                    }

                    tr.Commit();
                }
            }

            ed.WriteMessage(string.Format("\n[HVAC] Đã chèn {0} thành công.", blockFileName));
        }
    }
}
