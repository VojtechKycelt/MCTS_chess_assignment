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

        public int winCount = 0; // Number of wins this node found
        public int visitedCount = 0; // Number of times this node has been visited

        public bool isMyTurn;

        public MCTSNode(Board board, MoveGenerator moveGenerator, bool isMyTurn, MCTSNode parent = null)
        {
            this.board = board.Clone();
            this.moveGenerator = moveGenerator;
            this.isMyTurn = isMyTurn;
            this.parent = parent;
            this.children = new List<MCTSNode>();
            this.unexploredMoves = moveGenerator.GenerateMoves(this.board, this.parent == null);
        }

        // Expands this node by exploring one of the unexplored moves
        public void Expand()
        {
            if (unexploredMoves.Count > 0)
            {
                Move move = unexploredMoves[0];
                Board newBoard = board.Clone();
                newBoard.MakeMove(move);
                MCTSNode childNode = new MCTSNode(newBoard, moveGenerator, !this.isMyTurn, this);
                children.Add(childNode);
                unexploredMoves.Remove(move);
            }
        }

        // Selects the best child node based on UCT
        public MCTSNode SelectChild()
        {
            const double C = 1; // Exploration constant, can be tuned
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
        public void Backpropagate(int result)
        {
            visitedCount++;

            // Update the win count (positive for wins, negative for losses)
            if (isMyTurn)
                winCount += result;
            else
                winCount -= result;

            parent?.Backpropagate(result);
        }

        // Run a simulation (random game) from this node until a terminal state is reached
        public int Simulate()
        {
            Board simulationBoard = board.Clone();
            bool turn = isMyTurn;


            /* idk how to type this condition
            while (evaluation.Evaluate(simulationBoard) != -1)
            {
                var legalMoves = moveGenerator.GenerateMoves(simulationBoard,parent = null);
                Move randomMove = legalMoves[UnityEngine.Random.Range(0, legalMoves.Count)];
                simulationBoard = simulationBoard.MakeMove(move); // Apply random move
                turn = !turn;
            }
            */
            return evaluation.Evaluate(simulationBoard);
        }

    }
}