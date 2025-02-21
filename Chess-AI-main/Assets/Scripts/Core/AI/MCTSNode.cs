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
            if (children.Count == 0)
                return this;

            foreach (var child in children)
                if (child.visitedCount > 0)
                    child.UCTValue = computeUCTValue(child);

            return children
                .Select((child, index) => new { child, index }) // Add index to preserve order
                .OrderByDescending(x => x.child.UCTValue)       // Sort by UCTValue
                .ThenBy(x => x.index)                           // Maintain original order for ties
                .FirstOrDefault().child;                       // Select the first one
            //return children.OrderByDescending(child => child.UCTValue).FirstOrDefault();
        }

        public MCTSNode Expand()
        {
            if (visitedCount == 0 && parent != null)
                return this;
            for (int i = unexploredMoves.Count - 1; i >= 0; i--)
            {
                Move move = unexploredMoves[i];
                Board newBoard = board.Clone();
                newBoard.MakeMove(move);
                MCTSNode childNode = new MCTSNode(newBoard, moveGenerator, rand, evaluation, initialMove, !this.isMyTurn, !this.team, this);
                if (this.parent == null) //if we are in root - set initial move to this move
                    childNode.initialMove = move;
                children.Add(childNode);
            }
            unexploredMoves.Clear();
            return children.FirstOrDefault();
        }

        public float Simulate(int playoutDepthLimit)
        {
            SimPiece[,] simState = board.GetLightweightClone();
            int simulationDepth = 0;
            bool isMyTurnSimulation = isMyTurn;
            bool teamToMove = team;

            while (simulationDepth < playoutDepthLimit)
            {
                List<SimMove> possibleMoves = moveGenerator.GetSimMoves(simState, teamToMove);

                if (possibleMoves.Count == 0)
                    break;

                /*if (possibleMoves.Count == 1)
                {
                    //return win immediately because only 1 possibleMove means capturing king
                    //could be draw or only one piece i guess
                    return isMyTurnSimulation ? 1.0f : 0.0f;
                }*/

                SimMove selectedMove = possibleMoves[rand.Next(possibleMoves.Count)];
                ApplySimMove(simState, selectedMove);
                if (IsKingCaptured(simState))
                    return isMyTurnSimulation ? 1.0f : 0.0f;
                
                //switch turns
                isMyTurnSimulation = !isMyTurnSimulation;
                teamToMove = !teamToMove;
                simulationDepth++;
            }
            return evaluation.EvaluateSimBoard(simState, teamToMove);
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
            //return true
            return !(whiteKingExists && blackKingExists); // True if one of the kings captured
        }

        // Backpropagate the result of a simulation to this node and its ancestors
        public void Backpropagate(float result)
        {
            visitedCount++;
            rewards += result;
            parent?.Backpropagate(result);
        }

        private double computeUCTValue(MCTSNode child)
        {
            if (child.parent == null) 
                return 0;
            return ((double)child.rewards / child.visitedCount) + C * Mathf.Sqrt(Mathf.Log(child.parent.visitedCount) / child.visitedCount);
        }

    }
}