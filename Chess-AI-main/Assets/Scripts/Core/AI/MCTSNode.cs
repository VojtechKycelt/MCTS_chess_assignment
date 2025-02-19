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
        public float rewards = 0; // Number of wins this node found
        public int visitedCount = 0; // Number of times this node has been visited
        public double UCTValue = 0;
        public bool isMyTurn;
        System.Random rand;
        const double C = 1;

        public MCTSNode(Board board, MoveGenerator moveGenerator, Evaluation evaluation, Move initialMove, bool isMyTurn, MCTSNode parent = null)
        {
            this.board = board.Clone();
            this.moveGenerator = moveGenerator;
            this.evaluation = evaluation;
            this.initialMove = initialMove;
            this.isMyTurn = isMyTurn;
            this.parent = parent;
            this.children = new List<MCTSNode>();
            this.unexploredMoves = moveGenerator.GenerateMoves(this.board, this.parent == null);
            rand = new System.Random();

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

                double uctValue = computeUCTValue(child);

                if (uctValue > bestUCTValue)
                {
                    bestUCTValue = uctValue;
                    selectedChild = child;
                }
            }

            return selectedChild;
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
                MCTSNode childNode = new MCTSNode(newBoard, moveGenerator,evaluation, initialMove, !this.isMyTurn, this);
                if (this.parent == null) //if we are in root - set initial move to this move
                {
                    childNode.initialMove = move;
                }
                children.Add(childNode);
                unexploredMoves.RemoveAt(lastUnexploredMoveIndex); 
                return childNode;
            } else
            {
                Debug.Log("NO UNEXPLORED MOVES LEFT");
            }
            return null;
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
                Debug.Log("simulationDepth: " + simulationDepth);

                // Generate possible sim moves for the current state
                List<SimMove> possibleMoves = moveGenerator.GetSimMoves(simState, isEnemyTurn);

                // Check if there are no possible moves (end of simulation)
                if (possibleMoves.Count == 0)
                {
                    break;
                }

                if (possibleMoves.Count == 1)
                {
                    Debug.Log("FOUND END STATE");
                    //return win immediately because only 1 possibleMove means capturing king
                    return !isEnemyTurn ? 1.0f : 0.0f;
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
            //TODO: Maybe just return result = evaluation.EvaluateSimBoard(simState, isEnemyTurn); every time?
            Debug.Log("Returning result");

            float result;
            if (hasKingBeenCaptured || IsKingCaptured(simState))
            {
                result = !isEnemyTurn ? 1.0f : 0.0f; // Win for the current player if the opponent's king is captured
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
            //return true
            return !(whiteKingExists && blackKingExists); // True if one of the kings is missing
        }

        // Backpropagate the result of a simulation to this node and its ancestors
        public void Backpropagate(float result)
        {
            Debug.Log("BACKPROPAGATING");
            visitedCount++;  
            rewards += result;
            UCTValue = computeUCTValue(this);
            parent?.Backpropagate(result);
        }

        private double computeUCTValue(MCTSNode child)
        {
            if (child.parent == null) return 0;

            return ((double)child.rewards / child.visitedCount) + C * Mathf.Sqrt(Mathf.Log(child.parent.visitedCount) / child.visitedCount);
        }

    }
}