#define DEBUGON

using System;
using XMLEngine.Drawing;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using XML.Client.Tools.AStarEx;

namespace XMLEngine.Tools.AStar 
{
    public class PathFinderFast : IPathFinder
    {
        #region Structs
	    [StructLayout(LayoutKind.Sequential, Pack=1)] 
        internal struct PathFinderNodeFast
        {
            #region Variables Declaration
            public int     F; 
            public int     G;
            public ushort  PX; 
            public ushort  PY;
            public byte    Status;
            #endregion
        }
        #endregion

        #region Events
        public event PathFinderDebugHandler PathFinderDebug;
        #endregion

        #region Variables Declaration
        
        private byte[,]                         mGrid                   = null;
        private PriorityQueueB<int>             mOpen                   = null;
        private List<PathFinderNode>            mClose                  = new List<PathFinderNode>();
        private bool                            mStop                   = false;
        private bool                            mStopped                = true;
        private int                             mHoriz                  = 0;
        
        private HeuristicFormula mFormula = HeuristicFormula.EuclideanNoSQR;
        private bool                            mDiagonals              = true;
        private int                             mHEstimate              = 10;
        private bool                            mPunishChangeDirection  = false;
        private bool                            mReopenCloseNodes       = true;
        private bool                            mTieBreaker             = false;
        private bool                            mHeavyDiagonals         = false;
        private int                             mSearchLimit            = 40000;
        private double                          mCompletedTime          = 0;
        private bool                            mDebugProgress          = false;
        private bool                            mDebugFoundPath         = false;
        private static PathFinderNodeFast[]     mCalcGrid               = null;
        private byte                            mOpenNodeValue          = 1;
        private byte                            mCloseNodeValue         = 2;
        private int[,] mPunish = null;
        private int mMaxNum = 0;
        private bool mEnablePunish = false;

        private byte[,] mPointCost = null;
        private bool bPointCostDirty = true;
        
        
        private int                             mH                      = 0;
        private int                             mLocation               = 0;
        private int                             mNewLocation            = 0;
        private ushort                          mLocationX              = 0;
        private ushort                          mLocationY              = 0;
        private ushort                          mNewLocationX           = 0;
        private ushort                          mNewLocationY           = 0;
        private int                             mCloseNodeCounter       = 0;
        private ushort                          mGridX                  = 0;
        private ushort                          mGridY                  = 0;
        private ushort                          mGridXMinus1            = 0;
        private ushort                          mGridYLog2              = 0;
        private bool                            mFound                  = false;
        private sbyte[,]                        mDirection              = new sbyte[8,2]{{0,-1} , {1,0}, {0,1}, {-1,0}, {1,-1}, {1,1}, {-1,1}, {-1,-1}};
        private sbyte[,]                        mDirection2             = new sbyte[16, 2] { { -2, 2 }, { -2, 1 }, { -2, 0 }, { -2, -1 }, { -2, -2 }, { 2, 2 }, { 2, 1 }, { 2, 0 }, { 2, -1 }, { 2, -2 }, { -1, -2 }, { 0, -2 }, { 1, -2 }, { -1, 2 }, { 0, 2 }, { 1, 2 }};
        private int[]                           nDirectionCost = new int[8] { 10, 10, 10, 10, 14, 14, 14, 14 };
        private int                             mEndLocation            = 0;
        private int                             mNewG                   = 0;
        #endregion

        #region Constructors
        public PathFinderFast(byte[,] grid)
        {
            if (grid == null)
                throw new Exception("Grid cannot be null");

            mGrid           = grid;
            mGridX          = (ushort) (mGrid.GetUpperBound(0) + 1);
            mGridY          = (ushort) (mGrid.GetUpperBound(1) + 1);
            mGridXMinus1    = (ushort) (mGridX - 1);
            mGridYLog2      = (ushort) Math.Log(mGridX, 2);

            
            if (Math.Log(mGridX, 2) != (int) Math.Log(mGridX, 2) ||
                Math.Log(mGridY, 2) != (int) Math.Log(mGridY, 2))
                throw new Exception("Invalid Grid, size in X and Y must be power of 2");

            
            if (mCalcGrid == null || mCalcGrid.Length < (mGridX * mGridY))
            {
                mCalcGrid = new PathFinderNodeFast[mGridX * mGridY];
            }

            mOpen   = new PriorityQueueB<int>(new ComparePFNodeMatrix(mCalcGrid));

            ResetPointCost();
        }

        #endregion

        #region Properties
        public bool Stopped
        {
            get { return mStopped; }
        }

        public HeuristicFormula Formula
        {
            get { return mFormula; }
            set { mFormula = value; }
        }

        
        
        
        public bool bCheckPointCost = true;

        public bool Diagonals
        {
            get { return mDiagonals; }
            set 
            { 
                mDiagonals = value; 
                if (mDiagonals)
                    mDirection = new sbyte[8,2]{{0,-1} , {1,0}, {0,1}, {-1,0}, {1,-1}, {1,1}, {-1,1}, {-1,-1}};
                else
                    mDirection = new sbyte[4,2]{{0,-1} , {1,0}, {0,1}, {-1,0}};
            }
        }

        public bool HeavyDiagonals
        {
            get { return mHeavyDiagonals; }
            set { mHeavyDiagonals = value; }
        }

        public int HeuristicEstimate
        {
            get { return mHEstimate; }
            set { mHEstimate = value; }
        }

        public bool PunishChangeDirection
        {
            get { return mPunishChangeDirection; }
            set { mPunishChangeDirection = value; }
        }

        public bool ReopenCloseNodes
        {
            get { return mReopenCloseNodes; }
            set { mReopenCloseNodes = value; }
        }

        public bool TieBreaker
        {
            get { return mTieBreaker; }
            set { mTieBreaker = value; }
        }

        public int SearchLimit
        {
            get { return mSearchLimit; }
            set { mSearchLimit = value; }
        }

        public double CompletedTime
        {
            get { return mCompletedTime; }
            set { mCompletedTime = value; }
        }

        public bool DebugProgress
        {
            get { return mDebugProgress; }
            set { mDebugProgress = value; }
        }

        public bool DebugFoundPath
        {
            get { return mDebugFoundPath; }
            set { mDebugFoundPath = value; }
        }

        public int[,] Punish
        {
            get { return mPunish; }
            set { mPunish = value; }
        }

        public int MaxNum
        {
            get { return mMaxNum; }
            set { mMaxNum = value; }
        }

        public bool EnablePunish
        {
            get { return mEnablePunish; }
            set { mEnablePunish = value; }
        }

        #endregion

        #region Methods
        public void FindPathStop()
        {
            mStop = true;
        }

        public List<PathFinderNode> FindPath(Point start, Point end, int nSearchLimit)
        {
            return FindPath(new Point2D((int)start.X, (int)start.Y), new Point2D((int)end.X, (int)end.Y), nSearchLimit);
        }

        private int GetPunishNum(int x, int y)
        {
            if (!mEnablePunish) return 0;
            if (null == mPunish) return 0;
            return mMaxNum - Math.Min(mPunish[x, y], 3);
        }

        public List<PathFinderNode> FindPath(Point2D start, Point2D end, int nSearchLimit)
        {
            
            

            lock (this)
            {
				long ticks = DateTime.Now.Ticks;

                mSearchLimit = nSearchLimit;
				
                
                Array.Clear(mCalcGrid, 0, mCalcGrid.Length);

                mFound              = false;
                mStop               = false;
                mStopped            = false;
                mCloseNodeCounter   = 0;
                
                
                mOpen.Clear();
                mClose.Clear();

                #if DEBUGON
                if (mDebugProgress && PathFinderDebug != null)
                    PathFinderDebug(0, 0, start.X, start.Y, PathFinderNodeType.Start, -1, -1);
                if (mDebugProgress && PathFinderDebug != null)
                    PathFinderDebug(0, 0, end.X, end.Y, PathFinderNodeType.End, -1, -1);
                #endif

                mLocation                      = (start.Y << mGridYLog2) + start.X;
                mEndLocation                   = (end.Y << mGridYLog2) + end.X;
                mCalcGrid[mLocation].G         = 0;
                mCalcGrid[mLocation].F         = mHEstimate;
                mCalcGrid[mLocation].PX        = (ushort) start.X;
                mCalcGrid[mLocation].PY        = (ushort) start.Y;
                mCalcGrid[mLocation].Status    = mOpenNodeValue;

                mOpen.Push(mLocation);
                while(mOpen.Count > 0 && !mStop)
                {
                    mLocation    = mOpen.Pop();

                    
                    if (mCalcGrid[mLocation].Status == mCloseNodeValue)
                        continue;

                    mLocationX   = (ushort) (mLocation & mGridXMinus1);
                    mLocationY   = (ushort) (mLocation >> mGridYLog2);
                    
                    #if DEBUGON
                    if (mDebugProgress && PathFinderDebug != null)
                        PathFinderDebug(0, 0, mLocation & mGridXMinus1, mLocation >> mGridYLog2, PathFinderNodeType.Current, -1, -1);
                    #endif

                    if (mLocation == mEndLocation)
                    {
                        mCalcGrid[mLocation].Status = mCloseNodeValue;
                        mFound = true;
                        break;
                    }

                    if (mCloseNodeCounter > mSearchLimit)
                    {
                        mStopped = true;
                        return null;
                    }

                    if (mPunishChangeDirection)
                        mHoriz = (mLocationX - mCalcGrid[mLocation].PX); 

                    
                    for (int i=0; i<(mDiagonals ? 8 : 4); i++)
                    {
                        mNewLocationX = (ushort) (mLocationX + mDirection[i,0]);
                        mNewLocationY = (ushort) (mLocationY + mDirection[i,1]);
                        mNewLocation  = (mNewLocationY << mGridYLog2) + mNewLocationX;

                        if (mNewLocationX >= mGridX || mNewLocationY >= mGridY)
                            continue;

                        if (mCalcGrid[mNewLocation].Status == mCloseNodeValue && !mReopenCloseNodes)
                            continue;

                        
                        if (mGrid[mNewLocationX, mNewLocationY] == 0)
                            continue;

                        if (mHeavyDiagonals && i > 3)
                            mNewG = mCalcGrid[mLocation].G + (int)(mGrid[mNewLocationX, mNewLocationY] * 2.41);
                        else
                            mNewG = mCalcGrid[mLocation].G + GetDistanceCost(mLocationX, mLocationY, mNewLocationX, mNewLocationY);

                        if (mPunishChangeDirection)
                        {
                            if ((mNewLocationX - mLocationX) != 0)
                            {
                                if (mHoriz == 0)
                                    mNewG += Math.Abs(mNewLocationX - end.X) + Math.Abs(mNewLocationY - end.Y);
                            }
                            if ((mNewLocationY - mLocationY) != 0)
                            {
                                if (mHoriz != 0)
                                    mNewG += Math.Abs(mNewLocationX - end.X) + Math.Abs(mNewLocationY - end.Y);
                            }
                        }

                        mNewG = mNewG + GetPunishNum(mNewLocationX, mNewLocationY);

                        
                        if (mCalcGrid[mNewLocation].Status == mOpenNodeValue || mCalcGrid[mNewLocation].Status == mCloseNodeValue)
                        {
                            
                            if (mCalcGrid[mNewLocation].G <= mNewG)
                                continue;
                        }

                        mCalcGrid[mNewLocation].PX      = mLocationX;
                        mCalcGrid[mNewLocation].PY      = mLocationY;
                        mCalcGrid[mNewLocation].G       = mNewG;

                        mH = GetDistanceCost(mNewLocationX, mNewLocationY, end.X, end.Y);
                        
                        if (mTieBreaker)
                        {
                            int dx1 = mLocationX - end.X;
                            int dy1 = mLocationY - end.Y;
                            int dx2 = start.X - end.X;
                            int dy2 = start.Y - end.Y;
                            int cross = Math.Abs(dx1 * dy2 - dx2 * dy1);
                            mH = (int)(mH + cross * 0.001);
                        }
                        mCalcGrid[mNewLocation].F = mNewG + mH;

                        #if DEBUGON
                        if (mDebugProgress && PathFinderDebug != null)
                            PathFinderDebug(mLocationX, mLocationY, mNewLocationX, mNewLocationY, PathFinderNodeType.Open, mCalcGrid[mNewLocation].F, mCalcGrid[mNewLocation].G);
                        #endif

                        
                        
                        
                        
                        
                        
                        
                        

                        
                        
                            mOpen.Push(mNewLocation);
                        
                        mCalcGrid[mNewLocation].Status = mOpenNodeValue;
                    }

                    mCloseNodeCounter++;
                    mCalcGrid[mLocation].Status = mCloseNodeValue;

                    #if DEBUGON
                    if (mDebugProgress && PathFinderDebug != null)
                        PathFinderDebug(0, 0, mLocationX, mLocationY, PathFinderNodeType.Close, mCalcGrid[mLocation].F, mCalcGrid[mLocation].G);
                    #endif
                }
				
				long elapsedTicks = (DateTime.Now.Ticks - ticks) / 10000L;

                if (mFound)
                {
                    mClose.Clear();
                    int posX = end.X;
                    int posY = end.Y;

                    PathFinderNodeFast fNodeTmp = mCalcGrid[(end.Y << mGridYLog2) + end.X];
                    PathFinderNode fNode;
                    fNode.F  = fNodeTmp.F;
                    fNode.G  = fNodeTmp.G;
                    fNode.H  = 0;
                    fNode.PX = fNodeTmp.PX;
                    fNode.PY = fNodeTmp.PY;
                    fNode.X  = end.X;
                    fNode.Y  = end.Y;

                    while(fNode.X != fNode.PX || fNode.Y != fNode.PY)
                    {
                        mClose.Insert(0, fNode);
                        #if DEBUGON
                        if (mDebugFoundPath && PathFinderDebug != null)
                            PathFinderDebug(fNode.PX, fNode.PY, fNode.X, fNode.Y, PathFinderNodeType.Path, fNode.F, fNode.G);
                        #endif
                        posX = fNode.PX;
                        posY = fNode.PY;
                        fNodeTmp = mCalcGrid[(posY << mGridYLog2) + posX];
                        fNode.F  = fNodeTmp.F;
                        fNode.G  = fNodeTmp.G;
                        fNode.H  = 0;
                        fNode.PX = fNodeTmp.PX;
                        fNode.PY = fNodeTmp.PY;
                        fNode.X  = posX;
                        fNode.Y  = posY;
                    }

                    mClose.Insert(0, fNode);
                    #if DEBUGON
                    if (mDebugFoundPath && PathFinderDebug != null)
                        PathFinderDebug(fNode.PX, fNode.PY, fNode.X, fNode.Y, PathFinderNodeType.Path, fNode.F, fNode.G);
                    #endif

                    
                    mClose = Floyd(mClose, ref mGrid);

                    mStopped = true;
                    return mClose;
                }
                mStopped = true;
                return null;
            }
        }

        int GetDistanceCost(int x, int y, int toX, int toY)
        {
            int cost = 0;
            switch (mFormula)
            {
                default:
                case HeuristicFormula.Manhattan:
                    cost = mHEstimate * (Math.Abs(x - toX) + Math.Abs(y - toY));
                    break;
                case HeuristicFormula.MaxDXDY:
                    cost = mHEstimate * (Math.Max(Math.Abs(x - toX), Math.Abs(y - toY)));
                    break;
                case HeuristicFormula.DiagonalShortCut:
                    int h_diagonal = Math.Min(Math.Abs(x - toX), Math.Abs(y - toY));
                    int h_straight = (Math.Abs(x - toX) + Math.Abs(y - toY));
                    cost = (mHEstimate * 2) * h_diagonal + mHEstimate * (h_straight - 2 * h_diagonal);
                    break;
                case HeuristicFormula.Euclidean:
                    cost = (int)(mHEstimate * Math.Sqrt(Math.Pow((x - toX), 2) + Math.Pow((y - toY), 2)));
                    break;
                case HeuristicFormula.EuclideanNoSQR:
                    cost = (int)(mHEstimate * (Math.Pow((x - toX), 2) + Math.Pow((y - toY), 2)));
                    break;
                case HeuristicFormula.Custom1:
                    Point2D dxy = new Point2D(Math.Abs(toX - x), Math.Abs(toY - y));
                    int Orthogonal = Math.Abs(dxy.X - dxy.Y);
                    int Diagonal = Math.Abs(((dxy.X + dxy.Y) - Orthogonal) / 2);
                    cost = mHEstimate * (Diagonal + Orthogonal + dxy.X + dxy.Y);
                    break;
                case HeuristicFormula.Custom2:
                    int cntX = Math.Abs(x - toX);
                    int cntY = Math.Abs(y - toY);
                    if(cntX > cntY)
                    {
                        cost = (int)(mHEstimate * 1.4 * cntY) + mHEstimate * (cntX - cntY);
                    }
                    else
                    {
                        cost = (int)(mHEstimate * 1.4 * cntX) + mHEstimate * (cntY - cntX);
                    }
                    
                    cost += mHEstimate * GetPointCost(toX, toY);

                    break;
            }
            return cost;
        }

        int GetPointCost(int x, int y)
        {
            if(mPointCost == null || !bCheckPointCost)
            {
                return 0;
            }

            CheckPointCost();

            int cost = mPointCost[x, y];
            if(cost != 255)
                return cost;

            float count = 0;
            int newX = 0;
            int newY = 0;
            for (int i = 0; i < 8; i++)
            {
                newX = (ushort)(x + mDirection[i, 0]);
                newY = (ushort)(y + mDirection[i, 1]);
                if (newX >= mGridX || newY >= mGridY || newX < 0 || newY < 0)
                    continue;
                if(mGrid[newX,newY] == 0)
                {
                    count++;
                }
            }

            for (int i = 0; i < 16; i++)
            {
                newX = (ushort)(x + mDirection2[i, 0]);
                newY = (ushort)(y + mDirection2[i, 1]);
                if (newX >= mGridX || newY >= mGridY || newX < 0 || newY < 0)
                    continue;
                if (mGrid[newX, newY] == 0)
                {
                    count+=0.5f;
                }
            }

            if (count >= 4)
            {
                mPointCost[x, y] = (byte)count;
                return (int)count;
            }

            mPointCost[x, y] = 0;
            return 0;
        }

        public void ResetPointCost()
        {
            if (mPointCost == null)
            {
                mPointCost = new byte[mGridX, mGridY];
            }
            bPointCostDirty = true;
        }

        public void CheckPointCost()
        {
            if (bPointCostDirty)
            {
                bPointCostDirty = false; 
                for (int i = 0; i < mGridX; i++)
                    for (int j = 0; j < mGridY; j++)
                    {
                        mPointCost[i, j] = 255;
                    }
            }
        }
        #endregion

        #region Inner Classes
        internal class ComparePFNodeMatrix : IComparer<int>
        {
            #region Variables Declaration
            PathFinderNodeFast[] mMatrix;
            #endregion

            #region Constructors
            public ComparePFNodeMatrix(PathFinderNodeFast[] matrix)
            {
                mMatrix = matrix;
            }
            #endregion

            #region IComparer Members
            public int Compare(int a, int b)
            {
                int aF = mMatrix[a].F;
                int bF = mMatrix[b].F;
                if (aF > bF)
                    return 1;
                else if (aF < bF)
                    return -1;
                return 0;
            }
            #endregion
        }
        #endregion

        #region

        
        public static List<PathFinderNode> Floyd(List<PathFinderNode> _floydPath, ref byte[,] grid)
        {
            if (null == _floydPath || _floydPath.Count <= 0)
            {
                return null;
            }

            

            int len = _floydPath.Count;
            if (len > 2)
            {
                PathFinderNode vector = new PathFinderNode();
                PathFinderNode tempVector = new PathFinderNode();

                
                
                
                FloydVector(ref vector, _floydPath[len - 1], _floydPath[len - 2]);

                for (int i = _floydPath.Count - 3; i >= 0; i--)
                {
                    FloydVector(ref tempVector, _floydPath[i + 1], _floydPath[i]);
					
                    if (vector.X == tempVector.X && vector.Y == tempVector.Y)
                    {
                        _floydPath.RemoveAt(i + 1);
                    }
                    else
                    {
                        vector.X = tempVector.X;
                        vector.Y = tempVector.Y;
                    }
                }
            }

            
            
            
            
            len = _floydPath.Count;
            for (int i = len - 1; i >= 0; i--)
            {
                for (int j = 0; j <= i - 2; j++)
                {
                    if (hasBarrier(ref grid, _floydPath[i].X, _floydPath[i].Y, _floydPath[j].X, _floydPath[j].Y) == false)
                    {
                        for (int k = i - 1; k > j; k--)
                        {
                            _floydPath.RemoveAt(k);
                        }
                        i = j;
                        len = _floydPath.Count;
                        break;
                    }
                }
            }

            return _floydPath;
        }

        private static List<PathFinderNode> ReverseList(List<PathFinderNode> floydPath)
        {
            floydPath.Reverse(0, floydPath.Count);
            return floydPath;
        }

        private static void FloydVector(ref PathFinderNode target, PathFinderNode n1, PathFinderNode n2)
        {
            target.X = n1.X - n2.X;
            target.Y = n1.Y - n2.Y;
        }

        
        public static bool hasBarrier(ref byte[,] grid, int startX, int startY, int endX, int endY)
        {
            
            if( startX == endX && startY == endY )return false;
            if (endX < 0 || endX >= grid.GetUpperBound(0) || endY < 0 || endY >= grid.GetUpperBound(1)) return true;
            if (grid[endX, endY] == 0) return true;
			
            
            PointF point1 = new PointF( startX + 0.5f, startY + 0.5f );
            PointF point2 = new PointF( endX + 0.5f, endY + 0.5f );
			
            float distX = Math.Abs(endX - startX);
            float distY = Math.Abs(endY - startY);									
			
            
            bool loopDirection = distX > distY ? true : false;

            CalcLineHandler lineFuction = null;
					
            
            float i = 0;
			
            
            int loopStart = 0;
			
            
            int loopEnd = 0;
			
            
            List<ANode> nodesPassed = null;
            ANode elem = null;
			
            
            if( loopDirection )
            {				
                lineFuction = MathUtilX.getLineFunc(point1, point2, 0);
				
                loopStart = Math.Min( startX, endX );
                loopEnd = Math.Max( startX, endX );
				
                
                for( i=loopStart; i<=loopEnd; i++ )
                {
                    
                    
                    if( i==loopStart ) i += 0.5f;

                    
                    float yPos = lineFuction(i);

                    if (checkNodesUnderPoint(i, yPos, grid))
                        return true;
                    
                    
                    
                    
                    
                    

					
                    if( i == loopStart + 0.5f ) i -= 0.5f;
                }
            }
            else
            {
                lineFuction = MathUtilX.getLineFunc(point1, point2, 1);
				
                loopStart = Math.Min( startY, endY );
                loopEnd = Math.Max( startY, endY );
				
                
                for( i=loopStart; i<=loopEnd; i++ )
                {
                    if( i==loopStart ) i += 0.5f;

                    
                    float xPos = lineFuction(i);

                    if (checkNodesUnderPoint(xPos, i, grid))
                        return true;
                    
                    
                    
                    
                    
                    
										
                    if( i == loopStart + 0.5f ) i -= 0.5f;
                }
            }			
			
            return false;			
        }

        public static List<ANode> getNodesUnderPoint(float xPos, float yPos)
		{
			List<ANode> result = new List<ANode>();
			bool xIsInt = xPos % 1 == 0;
			bool yIsInt = yPos % 1 == 0;
			
			if( xIsInt && yIsInt )
			{
				result.Add(new ANode( (int)xPos - 1, (int)yPos - 1));
				result.Add(new ANode( (int)xPos, (int)yPos - 1));
				result.Add(new ANode( (int)xPos - 1, (int)yPos));
				result.Add(new ANode( (int)xPos, (int)yPos));
			}
				
				
			else if( xIsInt && !yIsInt )
			{
				result.Add(new ANode( (int)xPos - 1, (int)(yPos) ));
				result.Add(new ANode( (int)xPos, (int)(yPos) ));
			}
				
			else if( !xIsInt && yIsInt )
			{
				result.Add(new ANode( (int)(xPos), (int)yPos - 1 ));
				result.Add(new ANode( (int)(xPos), (int)yPos ));
			}
				
			else
			{
                result.Add(new ANode((int)(xPos), (int)(yPos)));
			}
			
			return result;
        }

        public static bool checkNodesUnderPoint(float xPos, float yPos, byte[,] grid)
        {
            bool xIsInt = xPos % 1 == 0;
            bool yIsInt = yPos % 1 == 0;

            
            if (xIsInt && yIsInt)
            {
                if (grid[(int)xPos - 1, (int)yPos - 1] == 0)
                    return true;
                if (grid[(int)xPos, (int)yPos - 1] == 0)
                    return true;
                if (grid[(int)xPos - 1, (int)yPos] == 0)
                    return true;
                if (grid[(int)xPos, (int)yPos] == 0)
                    return true;
            }
            
            
            else if (xIsInt && !yIsInt)
            {
                if (grid[(int)xPos - 1, (int)yPos] == 0)
                    return true;
                if (grid[(int)xPos, (int)yPos] == 0)
                    return true;
            }
            
            else if (!xIsInt && yIsInt)
            {
                if (grid[(int)xPos, (int)yPos - 1] == 0)
                    return true;
                if (grid[(int)xPos, (int)yPos] == 0)
                    return true;
            }
            
            else
            {
                if (grid[(int)xPos, (int)yPos] == 0)
                    return true;
            }

            return false;
        }

        #endregion
    }
}
