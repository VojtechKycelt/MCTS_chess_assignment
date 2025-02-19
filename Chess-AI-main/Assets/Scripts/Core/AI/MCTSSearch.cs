namespace Chess
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using UnityEngine;
    using static System.Math;
    using System.Linq; // Add this to access LINQ methods
    using UnityEditor.Animations;

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
            root = new MCTSNode(board, moveGenerator, evaluation, Move.InvalidMove, true);
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

            SearchMoves();
            foreach (MCTSNode node in root.children)
            {
                Debug.Log(node.UCTValue.ToString());
            }
            bestMove = root.children.OrderByDescending(child => child.UCTValue).FirstOrDefault().initialMove;
            Debug.Log("bestmove invalid: " + bestMove.IsInvalid);
            onSearchComplete?.Invoke(bestMove);

            if (!settings.useThreading)
            {
                LogDebugInfo();
            }
        }

        public void EndSearch()
        {
            Debug.Log("EndSearch method invoked");

            //WTF IS THIS ABOMINATION XD
            if (settings.useTimeLimit)
            {
                Debug.Log("abortSearch");

                abortSearch = true;
            }
        }

        void SearchMoves()
        {

            // Don't forget to end the search once the abortSearch parameter gets set to true.
            while (!abortSearch)
            {
                if (settings.limitNumOfPlayouts && numOfPlayouts >= settings.maxNumOfPlayouts)
                {
                    abortSearch = true;
                }
                Debug.Log("numOfPlayouts / maxNumOfPlayouts" + numOfPlayouts + " / " + settings.maxNumOfPlayouts);
                //1. selection
                MCTSNode selectedNodeToSimulate = root;
                int depth = 0;
                while (selectedNodeToSimulate.children.Count > 0)
                {
                    Debug.Log("selectedNodeToSimulate.children.Count: " + selectedNodeToSimulate.children.Count);
                    selectedNodeToSimulate = selectedNodeToSimulate.SelectChild();
                    depth++;
                }
                Debug.Log("depth: " + depth);
                if (selectedNodeToSimulate.visitedCount > 0)
                {
                    selectedNodeToSimulate.Expand();
                } 
                //2. expansion
                if (selectedNodeToSimulate != null)
                {
                    Debug.Log("SIMULATING");
                    // 3. simulation
                    float simulationResult = selectedNodeToSimulate.Simulate(settings.playoutDepthLimit);
                    numOfPlayouts++;
                    // 4. backpropagation
                    selectedNodeToSimulate.Backpropagate(simulationResult);
                    Debug.Log("backpropagated, now looking for bestMove");

                    Debug.Log("bestmove: " + bestMove);
                } else
                {
                    abortSearch = true;
                }
            }
            Debug.Log("Search Aborted");

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