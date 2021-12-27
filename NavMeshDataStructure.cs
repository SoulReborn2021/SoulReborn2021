
using System;
using System.Collections.Generic;
using UnityEngine;

    public enum PointSide
    {
        ON_LINE = 0,
        LEFT_SIDE = 1,
        RIGHT_SIDE = 2,
    };

    public enum LineCrossState
    {
        COLINE = 0,
        PARALLEL,
        CROSS,
        NOT_CROSS
    }

    public enum PolyResCode
    {
        Success = 0,
        ErrEmpty = -1,
        ErrNotCross = -2,
        ErrCrossNum = -3, 
        ErrNotInside = -4
    }

    public enum NavResCode
    {
        Success = 0,
        Failed = -1,
        NotFindDt = -2,
        FileNotExist = -3,
        VersionNotMatch = -4,
    }

    public enum PathResCode
    {
        Success = 0,
        Failed = -1,
        NoMeshData = -2,
        NoStartTriOrEndTri = -3,
        NavIdNotMatch = -4, 
        NotFoundPath = -5,
        CanNotGetNextWayPoint = -6,
        GroupNotMatch, 
        NoCrossPoint, 
    }

    public class WayPoint
    {
        public Vector2 Position { get; set; }
        public Triangle Triangle { get; set; }

        public WayPoint() { }

        public WayPoint(Vector2 pnt, Triangle tri)
        {
            Position = pnt;
            Triangle = tri;
        }
    }

    public class Line2D
    {
        private Vector2 pointStart;
        public UnityEngine.Vector2 PointStart
        {
            get { return pointStart; }
            set { pointStart = value; }
        }
        private Vector2 pointEnd;
        public UnityEngine.Vector2 PointEnd
        {
            get { return pointEnd; }
            set { pointEnd = value; }
        }
        public Line2D(Vector2 ps, Vector2 pe)
        {
            pointStart = ps;
            pointEnd = pe;
        }
        public Line2D()
        {
            pointEnd = pointStart = new Vector2();
        }

        
        
        
        
        
        
        
        public static bool CheckLineIn(List<Line2D> allLines, Line2D chkLine, out int index)
        {
            index = -1;
            for (int i = 0; i < allLines.Count; i++)
            {
                Line2D line = allLines[i];
                if (line.Equals(chkLine))
                {
                    index = i;
                    return true;
                }
            }
            return false;
        }

        public override bool Equals(object lineTemp)
        {
            Line2D line = (Line2D)lineTemp;
            if (line == null)
            {
                return false;
            }

            return (SGMath.IsEqual(pointStart , line.pointStart) && SGMath.IsEqual(pointEnd , line.pointEnd));
        }

        public override int GetHashCode() { return 0; }



        
        
        
        
        
        
        public PointSide classifyPoint(Vector2 point)
        {
            if (point == pointStart || point == pointEnd)
                return PointSide.ON_LINE;
            
            Vector2 vectorA = pointEnd - pointStart;
            
            Vector2 vectorB = point - pointStart;

            float crossResult = SGMath.CrossProduct(vectorA, vectorB);
            if (SGMath.IsEqualZero(crossResult))
                return PointSide.ON_LINE;
            else if (crossResult < 0)
                return PointSide.RIGHT_SIDE;
            else
                return PointSide.LEFT_SIDE;
        }

        
        
        
        
        
        public bool Equals(Line2D line)
        {
            
            if (SGMath.IsEqualZero(line.pointStart - line.pointEnd) ||
                SGMath.IsEqualZero(pointStart - pointEnd))
                return false;

            bool bEquals = SGMath.IsEqualZero(pointStart - line.pointStart) ? true : SGMath.IsEqualZero(pointStart - line.pointEnd);
            if (bEquals)
            {
                bEquals = SGMath.IsEqualZero(pointEnd - line.pointStart) ? true : SGMath.IsEqualZero(pointEnd - line.pointEnd);
            }
            return bEquals;
        }

        
        
        
        
        public Vector2 GetDirection()
        {
            Vector2 dir = pointEnd - pointStart;
            return dir;
        }

        
        
        
        
        
        
        public LineCrossState intersection(Line2D other, out Vector2 intersectPoint)
        {
            intersectPoint.x = intersectPoint.y = float.NaN;
            if (!SGMath.CheckCross(PointStart, pointEnd, other.PointStart, other.PointEnd))
                return LineCrossState.NOT_CROSS;

            double A1, B1, C1, A2, B2, C2;

            A1 = this.pointEnd.y - this.pointStart.y;
            B1 = this.pointStart.x - this.pointEnd.x;
            C1 = this.pointEnd.x * this.pointStart.y - this.pointStart.x * this.pointEnd.y;

            A2 = other.pointEnd.y - other.pointStart.y;
            B2 = other.pointStart.x - other.pointEnd.x;
            C2 = other.pointEnd.x * other.pointStart.y - other.pointStart.x * other.pointEnd.y;

            if (SGMath.IsEqualZero(A1 * B2 - B1 * A2))
            {
                if (SGMath.IsEqualZero((A1 + B1) * C2 - (A2 + B2) * C1))
                {
                    return LineCrossState.COLINE;
                }
                else
                {
                    return LineCrossState.PARALLEL;
                }
            }
            else
            {
                intersectPoint.x = (float)((B2 * C1 - B1 * C2) / (A2 * B1 - A1 * B2));
                intersectPoint.y = (float)((A1 * C2 - A2 * C1) / (A2 * B1 - A1 * B2));
                return LineCrossState.CROSS;
            }
        }

        public float Length()
        {
            return (float)Math.Sqrt(Math.Pow(pointStart.x - pointEnd.x, 2.0) + Math.Pow(pointStart.y - pointEnd.y, 2.0));
        }

    }

    public class Triangle
    {
        public Vector2[] Points { get; set; } 

        public int ID { get; set; } 

        public int Group { get; set; } 

        public int[] Neighbors { get; set; } 

        public Vector2 CenterPos { get; set; } 

        
        public int SessionID { get; set; }
        public int ParentId { get; set; }
        public bool IsOpen { get; set; }
        
        public double[] WallDistance { get; set; }

        
        public double HValue { get; set; }
        public double GValue { get; set; }
        public int ArrivalWallIndex { get; set; }
        
        public int OutWallIndex { get; set; }

        
        public Rect BoxCollider { get; set; }

        public Triangle()
        {
            InitData();
        }

        public Triangle(Vector2 pointA, Vector2 pointB, Vector2 pointC)
        {
            InitData();
            Points[0] = pointA;
            Points[1] = pointB;
            Points[2] = pointC;

            CalcCenter();
            CalcCollider();
        }

        
        
        
        public void CalcCollider()
        {
            if (Points[0] == Points[1] || Points[1] == Points[2] || Points[0] == Points[2])
                return;

            Rect collider = new Rect();
            collider.xMin = collider.xMax = Points[0].x;
            collider.yMin = collider.yMax = Points[0].y;
            for (int i = 1; i < 3; i++)
            {
                if (Points[i].x < collider.xMin)
                {
                    collider.xMin = Points[i].x;
                }
                else if (Points[i].x > collider.xMax)
                {
                    collider.xMax = Points[i].x;
                }

                if (Points[i].y < collider.yMin)
                {
                    collider.yMin = Points[i].y;
                }
                else if (Points[i].y > collider.yMax)
                {
                    collider.yMax = Points[i].y;
                }
            }

            BoxCollider = collider;
        }



        
        
        
        private void InitData()
        {
            Points = new Vector2[3];
            Neighbors = new int[3];
            WallDistance = new double[3];

            for (int i = 0; i < 3; i++)
            {
                Neighbors[i] = -1;
            }

            SessionID = -1;
            ParentId = -1;
            IsOpen = false;
            HValue = 0;
            GValue = 0;
            ArrivalWallIndex = -1;
        }

        
        
        
        private void CalcCenter()
        {
            Vector2 temp = new Vector2();
            temp.x = (Points[0].x + Points[1].x + Points[2].x) / 3;
            temp.y = (Points[0].y + Points[1].y + Points[2].y) / 3;
            CenterPos = temp;
        }

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        

        
        
        
        
        
        public Line2D GetSide(int sideIndex)
        {
            Line2D newSide;

            switch (sideIndex)
            {
                case 0:
                    newSide = new Line2D(Points[0], Points[1]);
                    break;
                case 1:
                    newSide = new Line2D(Points[1], Points[2]);
                    break;
                case 2:
                    newSide = new Line2D(Points[2], Points[0]);
                    break;
                default:
                    newSide = new Line2D(Points[0], Points[1]);
                    
                    break;
            }

            return newSide;
        }

        
        
        
        
        
        
        public bool isPointIn(int x,int y)
        {
            var pt = new Vector2((float)x, (float)y);
            if (BoxCollider.xMin != BoxCollider.xMax && !BoxCollider.Contains(pt))
                return false;

            PointSide resultA = GetSide(0).classifyPoint(pt);
            PointSide resultB = GetSide(1).classifyPoint(pt);
            PointSide resultC = GetSide(2).classifyPoint(pt);

            if (resultA == PointSide.ON_LINE || resultB == PointSide.ON_LINE || resultC == PointSide.ON_LINE)
            {
                return true;
            }
            else if (resultA == PointSide.RIGHT_SIDE && resultB == PointSide.RIGHT_SIDE && resultC == PointSide.RIGHT_SIDE)
            {
                return true;
            }
            return false;
        }

        
        
        
        
        
        public void calcHeuristic(Vector2 endPos)
        {
            double xDelta = Math.Abs(CenterPos.x - endPos.x);
            double yDelta = Math.Abs(CenterPos.y - endPos.y);
            HValue = Math.Sqrt(xDelta * xDelta + yDelta * yDelta);
        }

        public void calcWallDistance()
        {
            Vector2[] wallMidPoint = new Vector2[3];
            wallMidPoint[0] = new Vector2((Points[0].x + Points[1].x) / 2, (Points[0].y + Points[1].y) / 2);
            wallMidPoint[1] = new Vector2((Points[1].x + Points[2].x) / 2, (Points[1].y + Points[2].y) / 2);
            wallMidPoint[2] = new Vector2((Points[2].x + Points[0].x) / 2, (Points[2].y + Points[0].y) / 2);

            WallDistance[0] = Math.Sqrt((wallMidPoint[0].x - wallMidPoint[1].x) * (wallMidPoint[0].x - wallMidPoint[1].x)
                + (wallMidPoint[0].y - wallMidPoint[1].y) * (wallMidPoint[0].y - wallMidPoint[1].y));
            WallDistance[1] = Math.Sqrt((wallMidPoint[1].x - wallMidPoint[2].x) * (wallMidPoint[1].x - wallMidPoint[2].x)
                + (wallMidPoint[1].y - wallMidPoint[2].y) * (wallMidPoint[1].y - wallMidPoint[2].y));
            WallDistance[2] = Math.Sqrt((wallMidPoint[2].x - wallMidPoint[0].x) * (wallMidPoint[2].x - wallMidPoint[0].x)
                + (wallMidPoint[2].y - wallMidPoint[0].y) * (wallMidPoint[2].y - wallMidPoint[0].y));
        }


        
        
        
        
        public void SetArrivalWall(int arriId)
        {
            if (arriId == -1)
                return;

            ArrivalWallIndex = GetWallIndex(arriId);
        }

        
        
        
        
        
        public int GetWallIndex(int wallId)
        {
            for (int i = 0; i < 3; i++)
            {
                if (Neighbors[i] != -1 && Neighbors[i] == wallId)
                    return i;
            }
            return -1;
        }

        
        
        
        
        
        public double GetCost(int outWallId)
        {
            int outWallIndex = GetWallIndex(outWallId);
            if (ArrivalWallIndex == -1)
                return 0;
            else if (ArrivalWallIndex != 0)
                return WallDistance[1];
            else if (outWallIndex == 1)
                return WallDistance[0];
            else
                return WallDistance[2];
        }

        
        
        
        
        
        public int isNeighbor(Triangle triNext)
        {
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (GetSide(i).Equals(triNext.GetSide(j)))
                        return i;
                }
            }
            return -1;
        }


        
        
        
        public void ResetData()
        {
            SessionID = -1;
            ParentId = -1;
            IsOpen = false;
            HValue = 0;
            GValue = 0;
            ArrivalWallIndex = -1;
        }

        
        
        
        
        
        
        float area = 0;
        public float Area()
        {
            if (area == 0)
            {
                float a = (float)Math.Sqrt((Points[0].x - Points[1].x) * (Points[0].x - Points[1].x)
                                          + (Points[0].y - Points[1].y) * (Points[0].y - Points[1].y));
                float b = (float)Math.Sqrt((Points[1].x - Points[2].x) * (Points[1].x - Points[2].x)
                                          + (Points[1].y - Points[2].y) * (Points[1].y - Points[2].y));
                float c = (float)Math.Sqrt((Points[2].x - Points[0].x) * (Points[2].x - Points[0].x)
                                          + (Points[2].y - Points[0].y) * (Points[2].y - Points[0].y));
                float p = (a + b + c) / 2;
                area = (float)Math.Sqrt(p * (p - a) * (p - b) * (p - c));
            }

            return area;
        }
    }

    public class NavNode
    {
        public Vector2 vertex;  
        public bool passed;     
        public bool isMain;     
        public bool o;          
        public bool isIns;      
        public NavNode other;   
        public NavNode next;    

        public NavNode(Vector2 point, bool isin, bool bMain)
        {
            vertex = point;
            isIns = isin;
            isMain = bMain;
            passed = false;
            o = false;
        }
    }

    public class Polygon
    {
        public int tag { get; set; }
        public List<Vector2> allPoints { get; set; }

        public Polygon()
        {
            allPoints = new List<Vector2>();
            tag = 0;
        }

        public Polygon(List<Vector2> points)
        {
            allPoints = points;
            tag = 0;
        }

        
        
        
        
        public void DelRepeatPoint()
        {
            for (int i = 0; i < allPoints.Count; i++)
            {
                for (int j = i + 1; j < allPoints.Count; j++)
                {
                    if (SGMath.IsEqualZero(allPoints[i] - allPoints[j]))
                    {
                        allPoints.Remove(allPoints[j]);
                        j = i;
                    }
                }
            }
        }

        
        
        
        
        public void CW()
        {
            if (!IsCW())
            {
                allPoints.Reverse();
            }
        }

        
        
        
        
        public bool IsCW()
        {
            if (allPoints.Count <= 2)
                return false;

            
            
            Vector2 topPoint = allPoints[0];
            int topIndex = 0;
            for (int i = 1; i < allPoints.Count; i++)
            {
                Vector2 currPoint = allPoints[i];
                if ((topPoint.y > currPoint.y)
                    || ((topPoint.y == currPoint.y) && (topPoint.x > currPoint.x)))
                {
                    topPoint = currPoint;
                    topIndex = i;
                }
            }

            
            int preIndex = (topIndex - 1) >= 0 ? (topIndex - 1) : (allPoints.Count - 1);
            int nextIndex = (topIndex + 1) < allPoints.Count ? (topIndex + 1) : 0;

            Vector2 prePoint = allPoints[preIndex];
            Vector2 nextPoint = allPoints[nextIndex];

            
            float r = SGMath.CrossProduct((prePoint - topPoint), (nextPoint - topPoint));
            if (r > 0)
                return true;

            return false;
        }

        
        
        
        
        public Rect GetCoverRect()
        {
            Rect rect = new Rect(0, 0, 0, 0);

            for (int i = 0; i < allPoints.Count; i++)
            {
                Vector2 pt = allPoints[i];
                if( i == 0)
                {
                    rect.xMin = rect.xMax = pt.x;
                    rect.yMin = rect.yMax = pt.y;
                }
                if (rect.xMin > pt.x)
                    rect.xMin = pt.x;
                if (rect.xMax < pt.x)
                    rect.xMax = pt.x;
                if (rect.yMin > pt.y)
                    rect.yMin = pt.y;
                if (rect.yMax < pt.y)
                    rect.yMax = pt.y;
            }
            return rect;
        }

        
        
        
        
        
        
        
        public static PolyResCode GetNodeIndex(List<NavNode> nodeList, Vector2 point, out int pIndex)
        {
            pIndex = -1;
            for (int i = 0; i < nodeList.Count; i++)
            {
                NavNode node = nodeList[i];
                if (SGMath.Equals(node.vertex, point))
                {
                    pIndex = i;
                    return PolyResCode.Success;
                }
            }
            return PolyResCode.ErrNotInside;
        }

        
        
        
        
        
        
        public static PolyResCode IntersectPoint(List<NavNode> c0, List<NavNode> c1, out int nInsCnt)
        {
            nInsCnt = 0;

            NavNode startNode0 = c0[0];
            NavNode startNode1 = null;
            Line2D line0, line1;
            Vector2 insPoint;
            bool hasIns = false;

            while (startNode0 != null)
            {
                
                if (startNode0.next == null)
                    line0 = new Line2D(startNode0.vertex, c0[0].vertex);
                else
                    line0 = new Line2D(startNode0.vertex, startNode0.next.vertex);

                startNode1 = c1[0];
                hasIns = false;

                while (startNode1 != null)
                {
                    if (startNode1.next == null)
                        line1 = new Line2D(startNode1.vertex, c1[0].vertex);
                    else
                        line1 = new Line2D(startNode1.vertex, startNode1.next.vertex);

                    if (line0.intersection(line1, out insPoint) == LineCrossState.CROSS)
                    {
                        int insPotIndex = -1;
                        if (Polygon.GetNodeIndex(c0, insPoint, out insPotIndex) == PolyResCode.ErrNotInside)
                        {
                            nInsCnt++;
                            NavNode node0 = new NavNode(insPoint, true, true);
                            NavNode node1 = new NavNode(insPoint, true, false);

                            c0.Add(node0);
                            c1.Add(node1);

                            node0.other = node1;
                            node1.other = node0;

                            
                            node0.next = startNode0.next;
                            startNode0.next = node0;
                            node1.next = startNode1.next;
                            startNode1.next = node1;

                            if (line0.classifyPoint(line1.PointEnd) == PointSide.RIGHT_SIDE)
                            {
                                node0.o = true;
                                node1.o = true;
                            }

                            hasIns = true;
                            break;
                        }
                    }
                    startNode1 = startNode1.next;

                }
                if (!hasIns)
                    startNode0 = startNode0.next;

            }

            return PolyResCode.Success;
        }

        
        
        
        
        
        
        public PolyResCode Union(Polygon other, ref List<Polygon> polyRes)
        {
            if (allPoints.Count == 0 || other.allPoints.Count == 0)
                return PolyResCode.ErrEmpty;
            else if (!SGMath.CheckCross(GetCoverRect(), other.GetCoverRect()))
                return PolyResCode.ErrNotCross;

            
            
            

            List<NavNode> mainNode = new List<NavNode>();     
            List<NavNode> subNode = new List<NavNode>();      

            
            for (int i = 0; i < allPoints.Count; i++)
            {
                NavNode currNode = new NavNode(allPoints[i], false, true);
                if (i > 0)
                {
                    NavNode preNode = mainNode[i - 1];
                    preNode.next = currNode;
                }
                mainNode.Add(currNode);
            }

            
            for (int j = 0; j < other.allPoints.Count; j++)
            {
                NavNode currNode = new NavNode(other.allPoints[j], false, false);
                if (j > 0)
                {
                    NavNode preNode = subNode[j - 1];
                    preNode.next = currNode;
                }
                subNode.Add(currNode);
            }

            int insCnt = 0;
            PolyResCode result = Polygon.IntersectPoint(mainNode, subNode, out insCnt);
            if (result == PolyResCode.Success && insCnt > 0)
            {
                if (insCnt % 2 != 0)
                {
                    return PolyResCode.ErrCrossNum;
                }
                else
                {
                    PolyResCode linkRes = Polygon.LinkToPolygon(mainNode, subNode, ref polyRes);
                    return linkRes;
                }
            }

            return PolyResCode.ErrCrossNum;
        }

        
        
        
        
        
        
        public static PolyResCode UnionAll(ref List<Polygon> polys)
        {
            int tag = 1;

            for (int i = 0; i < polys.Count; i++)
                polys[i].CW();

            for (int i = 0; i < polys.Count; i++)
            {
                Polygon p1 = polys[i];
                for (int j = 0; j < polys.Count; j++)
                {
                    Polygon p2 = polys[j];
                    if (!p1.Equals(p2))
                    {
                        List<Polygon> polyResult = new List<Polygon>();
                        PolyResCode result = p1.Union(p2, ref polyResult);

                        if (result == PolyResCode.Success && polyResult.Count > 0)
                        {
                            polys.Remove(p1);
                            polys.Remove(p2);

                            for (int k = 0; k < polyResult.Count; k++)
                            {
                                Polygon poly = polyResult[k];
                                if ( !poly.IsCW())
                                    poly.tag = tag++;

                                polys.Add(poly);
                            }
                            i = -1;
                            break;
                        }
                    }
                }
            }
            return PolyResCode.Success;
        }

        
        
        
        
        
        
        
        private static PolyResCode LinkToPolygon(List<NavNode> mainNode, List<NavNode> subNode, ref List<Polygon> polyRes)
        {
            polyRes.Clear();
            for (int i = 0; i < mainNode.Count; i++)
            {
                NavNode currNode = mainNode[i];

                
                if (currNode.isIns && !currNode.passed)
                {
                    List<Vector2> points = new List<Vector2>();
                    while (currNode != null)
                    {
                        currNode.passed = true;

                        
                        if (currNode.isIns)
                        {
                            currNode.other.passed = true;

                            if (!currNode.o)
                            {
                                if (currNode.isMain)
                                    currNode = currNode.other;
                            }
                            else
                            {
                                
                                if (!currNode.isMain)
                                    currNode = currNode.other;
                            }
                        }

                        points.Add(currNode.vertex);

                        if (currNode.next == null)
                        {
                            if (currNode.isMain)
                                currNode = mainNode[0];
                            else
                                currNode = subNode[0];
                        }
                        else
                            currNode = currNode.next;

                        if (currNode.vertex == points[0])
                            break;
                    }

                    
                    Polygon poly = new Polygon(points);
                    poly.DelRepeatPoint();
                    polyRes.Add(poly);
                }
            }
            return PolyResCode.Success;
        }
        public bool isPointIn(Vector2 target)
        {
            int i, j = allPoints.Count - 1;
            bool oddNodes = false;

            for (i = 0; i < allPoints.Count; i++)
            {
                Vector2 point1 = allPoints[i];
                Vector2 point2 = allPoints[j];
                if (point1.y < target.y && point2.y >= target.y
                || point2.y < target.y && point1.y >= target.y)
                {
                    if (point1.x + (target.y - point1.y) / (point2.y - point1.y) * (point2.x - point1.x) < target.x)
                    {
                        oddNodes = !oddNodes;
                    }
                }
                j = i;
            }

            return oddNodes;
        }
    }

