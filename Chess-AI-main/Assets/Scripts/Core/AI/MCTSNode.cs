namespace Chess
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public class MCTSNode
    {
        public Board board;
        public MoveGenerator moveGenerator;
        public Evaluation evaluation;
        public MCTSNode parent;
        public List<MCTSNode> children; 
        public List<Move> unexploredMoves;
        public Move initialMove;    // The initial move that lead to this state 
        public float winCount = 0; // Number of wins this node found
        public int visitedCount = 0; // Number of times this node has been visited
        public double UCTValue = 0;
        public bool isMyTurn;
        System.Random rand;
        const double C = 1;

        public MCTSNode(Board board, MoveGenerator moveGenerator, Move initialMove, bool isMyTurn, MCTSNode parent = null)
        {
            this.board = board.Clone();
            this.moveGenerator = moveGenerator;
            this.isMyTurn = isMyTurn;
            this.initialMove = initialMove;
            this.parent = parent;
            this.children = new List<MCTSNode>();
            this.unexploredMoves = moveGenerator.GenerateMoves(this.board, this.parent == null);
            rand = new System.Random();

        }

        // Expands this node by exploring one of the unexplored moves
        public MCTSNode Expand()
        {
            if (unexploredMoves.Count > 0)
            {
                int lastUnexploredMoveIndex = unexploredMoves.Count - 1;
                Move move = unexploredMoves[lastUnexploredMoveIndex];
                Board newBoard = board.Clone();
                newBoard.MakeMove(move);
                MCTSNode childNode = new MCTSNode(newBoard, moveGenerator,initialMove, !this.isMyTurn, this);
                if (this.parent == null) //if we are in root - set initial move to this move
                {
                    childNode.initialMove = move;
                }
                children.Add(childNode);
                unexploredMoves.RemoveAt(lastUnexploredMoveIndex); 
                return childNode;
            }
            return null;
        }

        // Selects the best child node based on UCT 
        public MCTSNode SelectChild()
        {
            MCTSNode selectedChild = null;
            double bestUCTValue = double.NegativeInfinity;

            foreach (var child in children)
            {
                if (child.visitedCount == 0) // If a child node is unvisited, prioritize it
                    return child;

                double uctValue = (double)child.winCount / child.visitedCount +
                                  C * Math.Sqrt(Math.Log(visitedCount) / child.visitedCount);

                if (uctValue > bestUCTValue)
                {
                    bestUCTValue = uctValue;
                    selectedChild = child;
                }
            }

            return selectedChild;
        }

        // Backpropagate the result of a simulation to this node and its ancestors
        public void Backpropagate(float result)
        {
            visitedCount++;

            // Update the win count (positive for wins, negative for losses)
            if (isMyTurn)
                winCount += result;
            else
                winCount -= result;

            UpdateUCTValue();

            parent?.Backpropagate(result);
        }

        // Run a simulation (random game) from this node until a terminal state is reached
        public float Simulate(int playoutDepthLimit)
        {
            // Clone the board state using the lightweight clone method
            SimPiece[,] simState = board.GetLightweightClone();

            // Set up the simulation variables
            int simulationDepth = 0;
            bool isEnemyTurn = !isMyTurn;
            bool hasKingBeenCaptured = false;

            while (simulationDepth < playoutDepthLimit)
            {
                // Generate possible sim moves for the current state
                List<SimMove> possibleMoves = moveGenerator.GetSimMoves(simState, isEnemyTurn);

                // Check if there are no possible moves (end of simulation)
                if (possibleMoves.Count == 0)
                {
                    break;
                }

                // Randomly select a move to play out
                SimMove selectedMove = possibleMoves[rand.Next(possibleMoves.Count)];

                // Apply the selected move to the simulation state
                ApplySimMove(simState, selectedMove);

                // Check for king capture (end of game)
                if (IsKingCaptured(simState))
                {
                    hasKingBeenCaptured = true;
                    break;
                }

                // Switch turns
                isEnemyTurn = !isEnemyTurn;
                simulationDepth++;
            }

            // Evaluate the resulting state
            float result;
            if (hasKingBeenCaptured || IsKingCaptured(simState))
            {
                result = isEnemyTurn ? 1.0f : 0.0f; // Win for the current player if the opponent's king is captured
            }
            else
            {
                result = evaluation.EvaluateSimBoard(simState, isEnemyTurn); // Evaluate the board for intermediate results
            }

            return result;
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
                        return false; // Both kings are still on the board
                    }
                }
            }

            return !(whiteKingExists && blackKingExists); // True if one of the kings is missing
        }

        private void UpdateUCTValue()
        {
            float parentVisits = parent != null ? parent.visitedCount : 1;

            UCTValue = (double)winCount / visitedCount + C * Math.Sqrt(Math.Log(parentVisits) / visitedCount);

        }

    }
}