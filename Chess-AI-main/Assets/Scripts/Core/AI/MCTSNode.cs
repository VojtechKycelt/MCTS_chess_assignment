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

        /*public MCTSNode SelectChild2()
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
        }*/

        public MCTSNode SelectChild()
        {
            if (children.Count == 0)
                return this;

            MCTSNode bestChild = null;
            double bestValue = double.NegativeInfinity;
            int bestIndex = int.MaxValue; // Ensure first-encountered max is preferred

            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];
                if (child.visitedCount > 0)
                    child.UCTValue = computeUCTValue(child);

                if (child.UCTValue > bestValue || (child.UCTValue == bestValue && i < bestIndex))
                {
                    bestChild = child;
                    bestValue = child.UCTValue;
                    bestIndex = i;
                }
            }

            return bestChild;
        }

        public MCTSNode Expand()
        {
            if (visitedCount == 0 && parent != null)
                return this;
            int lastUnexploredMoveIndex = unexploredMoves.Count - 1;
            Move move = unexploredMoves[lastUnexploredMoveIndex];
            Board newBoard = board.Clone();
            newBoard.MakeMove(move);
            MCTSNode childNode = new MCTSNode(newBoard, moveGenerator, rand, evaluation, initialMove, !this.isMyTurn, !this.team, this);
            if (this.parent == null) //if we are in root - set initial move to this move
                childNode.initialMove = move;
            children.Add(childNode);
            unexploredMoves.RemoveAt(lastUnexploredMoveIndex);
            return childNode;


            /*for (int i = unexploredMoves.Count - 1; i >= 0; i--)
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
            return children.FirstOrDefault();*/
        }

        public float Simulate2(int playoutDepthLimit)
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

                if (possibleMoves.Count == 1)
                {
                    SimMove singleMove = possibleMoves[0];
                    ApplySimMove(simState, singleMove);
                    return IsKingCaptured(simState) ? (isMyTurnSimulation ? 1.0f : 0.0f) : evaluation.EvaluateSimBoard(simState, teamToMove);
                }

                SimMove selectedMove = possibleMoves[rand.Next(possibleMoves.Count)];
                //SimMove selectedMove = SelectMove(possibleMoves,simState,teamToMove);
                ApplySimMove(simState, selectedMove);

                if (IsKingCaptured(simState))
                    return isMyTurnSimulation ? 1.0f : 0.0f;

                // Switch turns
                isMyTurnSimulation = !isMyTurnSimulation;
                teamToMove = !teamToMove;
                simulationDepth++;
            }
            return evaluation.EvaluateSimBoard(simState, teamToMove);
        }

        SimMove SelectMove(List<SimMove> possibleMoves, SimPiece[,] simState, bool teamToMove)
        {
            if (possibleMoves.Count == 1)
                return possibleMoves[0];

            // Separate capturing moves and normal moves
            List<SimMove> capturingMoves = new List<SimMove>();
            List<SimMove> normalMoves = new List<SimMove>();

            foreach (var move in possibleMoves)
            {   if (IsCaptureKingMove(simState, move))
                    return move;
                if (IsCaptureMove(simState, move))
                    capturingMoves.Add(move);
                else
                    normalMoves.Add(move);
            }

            // Bias towards capturing moves but allow normal moves with some probability
            if (capturingMoves.Count > 0)
            {
                // Use ε-greedy: 80% of the time, choose capturing move; 20% pick a random move
                if (rand.NextDouble() < 0.8)
                    return capturingMoves[rand.Next(capturingMoves.Count)];
            }

            // If no capturing moves, pick normal move randomly
            return normalMoves[rand.Next(normalMoves.Count)];
        }

        // Check if a move is a capture
        bool IsCaptureMove(SimPiece[,] simState, SimMove move)
        {
            return simState[move.endCoord1, move.endCoord2] != null; // Piece is captured
        } 
        bool IsCaptureKingMove(SimPiece[,] simState, SimMove move)
        {
            SimPiece piece = simState[move.endCoord1, move.endCoord2];
            if (piece != null && piece.type == SimPieceType.King)
                return true;
            return false;
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