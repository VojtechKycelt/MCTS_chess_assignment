namespace Chess
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    public class MCTSNode
    {
        public Board board;
        public MoveGenerator moveGenerator;
        public Evaluation evaluation;
        public MCTSNode parent;
        public List<MCTSNode> children;
        public List<Move> unexploredMoves;
        public Move initialMove;
        public double rewards = 0;
        public int visitedCount = 0;
        public double UCTValue = double.PositiveInfinity;
        public bool isMyTurn;
        public bool team;
        System.Random rand;
        public double C = 1;// / Mathf.Sqrt(2);

        public MCTSNode(Board board, MoveGenerator moveGenerator, System.Random rand, Evaluation evaluation, Move initialMove, bool isMyTurn, bool team, MCTSNode parent = null)
        {
            this.board = board.Clone();
            this.moveGenerator = moveGenerator;
            this.rand = rand;
            this.evaluation = evaluation;
            this.initialMove = initialMove;
            this.isMyTurn = isMyTurn;
            this.team = team;
            this.parent = parent;
            this.children = new List<MCTSNode>();
            this.unexploredMoves = moveGenerator.GenerateMoves(this.board, this.parent == null);
        }

        public MCTSNode SelectChild()
        {
            foreach (MCTSNode child in children)
            {
                if (child.visitedCount == 0)
                    return child;
                child.UCTValue = computeUCTValue(child);
            }
            return children.OrderByDescending(x => x.UCTValue).FirstOrDefault();
        }

        public MCTSNode Expand()
        {
            if (unexploredMoves.Count == 0) return this;
            unexploredMoves.Reverse();
            foreach (Move unexploredMove in unexploredMoves)
            {
                Board newBoard = board.Clone();
                newBoard.MakeMove(unexploredMove);
                MCTSNode childNode = new MCTSNode(newBoard, moveGenerator, rand, evaluation, unexploredMove, !this.isMyTurn, !this.team, this);
                children.Add(childNode);
            }
            unexploredMoves.Clear();
            return children.FirstOrDefault();
        }

        public float Simulate(int playoutDepthLimit)
        {
            SimPiece[,] simState = board.GetLightweightClone();
            int simulationDepth = 0;
            bool isMyTurnSim = isMyTurn;
            bool teamSim = team;

            while (simulationDepth < playoutDepthLimit)
            {
                List<SimMove> possibleMoves = moveGenerator.GetSimMoves(simState, teamSim);
                ApplySimMove(simState, possibleMoves[rand.Next(possibleMoves.Count)]);

                if (IsKingCaptured(simState))
                {
                    //favor sooner wins - desperate attempts to pass one test
                    float score = 1 - (simulationDepth / playoutDepthLimit);
                    return isMyTurnSim ? score : 1 - score;
                }

                isMyTurnSim = !isMyTurnSim;
                teamSim = !teamSim;
                simulationDepth++;
            }
            return evaluation.EvaluateSimBoard(simState, team);

        }

        void ApplySimMove(SimPiece[,] simState, SimMove move)
        {
            SimPiece piece = simState[move.startCoord1, move.startCoord2];
            simState[move.startCoord1, move.startCoord2] = null;
            simState[move.endCoord1, move.endCoord2] = piece;
        }

        bool IsKingCaptured(SimPiece[,] simState)
        {
            bool whiteKingExists = false;
            bool blackKingExists = false;

            for (int row = 0; row < simState.GetLength(0); row++)
            {
                for (int col = 0; col < simState.GetLength(1); col++)
                {
                    SimPiece piece = simState[row, col];
                    if (piece != null && piece.type == SimPieceType.King)
                    {
                        if (simState[row, col].team)
                        {
                            whiteKingExists = true;
                        }
                        else
                        {
                            blackKingExists = true;
                        }
                    }

                    if (whiteKingExists && blackKingExists)
                    {
                        return false; // Both kings alive
                    }
                }
            }
            return !(whiteKingExists && blackKingExists); // True if one of the kings captured
        }

        public void Backpropagate(float result)
        {
            visitedCount++;
            rewards += result;
            //inverse of the result for parent (different player starting the turn from state)
            parent?.Backpropagate(1 - result);
        }

        private double computeUCTValue(MCTSNode child)
        {
            if (child.parent == null)
                return 0;
            return ((double)child.rewards / child.visitedCount) + C * Mathf.Sqrt(Mathf.Log(child.parent.visitedCount) / child.visitedCount);
        }
    }
}