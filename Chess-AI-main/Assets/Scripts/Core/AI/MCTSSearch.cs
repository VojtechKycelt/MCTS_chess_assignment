namespace Chess
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using UnityEngine;
    using static System.Math;
    using System.Linq; // Add this to access LINQ methods
    using UnityEditor.Animations;
    using UnityEditor.Experimental.GraphView;

    class MCTSSearch : ISearch
    {
        public event System.Action<Move> onSearchComplete;

        MoveGenerator moveGenerator;

        Move bestMove;
        int bestEval;
        bool abortSearch;

        MCTSSettings settings;
        Board board;
        Evaluation evaluation;
        bool team;
        System.Random rand;

        MCTSNode root;
        int numOfPlayouts;

        // Diagnostics
        public SearchDiagnostics Diagnostics { get; set; }
        System.Diagnostics.Stopwatch searchStopwatch;

        public MCTSSearch(Board board, MCTSSettings settings)
        {
            this.board = board;
            this.settings = settings;
            evaluation = new Evaluation();
            moveGenerator = new MoveGenerator();
            rand = new System.Random();
            team = board.WhiteToMove;
            root = new MCTSNode(board, moveGenerator, evaluation, Move.InvalidMove, true, team);
            Debug.Log("CONSTRUCTOR, whiteToMove: " + board.WhiteToMove.ToString());
        }

        public void StartSearch()
        {
            Debug.Log("STARTING SEARCH");
            InitDebugInfo();

            // Initialize search settings
            bestEval = 0;
            bestMove = Move.InvalidMove;
            numOfPlayouts = 0;

            moveGenerator.promotionsToGenerate = settings.promotionsToSearch;
            abortSearch = false;
            Diagnostics = new SearchDiagnostics();
            root = new MCTSNode(board, moveGenerator, evaluation, Move.InvalidMove, true, team);

            SearchMoves();
            Debug.Log("-------------------CHILDRENOS-------------------------");
            foreach (MCTSNode node in root.children)
            {
                Debug.Log("move: " + node.initialMove.Name + ", visitedCount: " + node.visitedCount + ", rewards: " + node.rewards + ", UCT: " + node.UCTValue.ToString());
            }
            bestMove = root.children.OrderByDescending(child => child.rewards).FirstOrDefault().initialMove;
            Debug.Log("bestMove: " + bestMove.Name);
            onSearchComplete?.Invoke(bestMove);

            if (!settings.useThreading)
            {
                LogDebugInfo();
            }
        }

        public void EndSearch()
        {
            if (settings.useTimeLimit)
            {
                abortSearch = true;
            }
        }

        void SearchMoves()
        {

            // Don't forget to end the search once the abortSearch parameter gets set to true.
            while (!abortSearch)
            {
                if (settings.limitNumOfPlayouts && numOfPlayouts >= settings.maxNumOfPlayouts)
                    abortSearch = true;
                //Debug.Log("numOfPlayouts / maxNumOfPlayouts" + numOfPlayouts + " / " + settings.maxNumOfPlayouts);

                
                MCTSNode selectedNodeToSimulate = root;
                
                //1. selection
                while (selectedNodeToSimulate.visitedCount > 0)
                {
                    selectedNodeToSimulate = selectedNodeToSimulate.SelectChild();
                    Debug.Log("selectedNodeToSimulate: " + selectedNodeToSimulate == null);
                }

                //2. expansion
                selectedNodeToSimulate.Expand();

                // 3. simulation
                float simulationResult = selectedNodeToSimulate.Simulate(settings.playoutDepthLimit);
                numOfPlayouts++;

                // 4. backpropagation
                selectedNodeToSimulate.Backpropagate(simulationResult);
            }
        }

        void LogDebugInfo()
        {
            // Optional
        }

        void InitDebugInfo()
        {
            searchStopwatch = System.Diagnostics.Stopwatch.StartNew();
            // Optional
        }
    }
}