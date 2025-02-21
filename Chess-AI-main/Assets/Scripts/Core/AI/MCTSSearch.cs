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

        System.Random rand;
        MoveGenerator moveGenerator;
        MCTSSettings settings;
        Evaluation evaluation;
        MCTSNode root;
        Board board;
        bool team;
        int numOfPlayouts;
        bool abortSearch;

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
        }

        public void StartSearch()
        {
            Debug.Log("STARTING SEARCH");

            InitDebugInfo();

            // Initialize search settings
            evaluation = new Evaluation();
            moveGenerator = new MoveGenerator();
            rand = new System.Random();
            team = board.WhiteToMove;
            numOfPlayouts = 0;
            moveGenerator.promotionsToGenerate = settings.promotionsToSearch;
            abortSearch = false;
            Diagnostics = new SearchDiagnostics();
            root = new MCTSNode(board, moveGenerator, evaluation, Move.InvalidMove, true, team);
            
            SearchMoves();

            //Debug.Log("-------------------CHILDREN-------------------------");

            Move bestMove = root.children.OrderByDescending(child => child.rewards).FirstOrDefault().initialMove;
            foreach (MCTSNode node in root.children.OrderByDescending(child => child.rewards))
            {
                Debug.Log("move: " + node.initialMove.Name + ", visitedCount: " + node.visitedCount + ", rewards: " + node.rewards + ", UCT: " + node.UCTValue.ToString());

            }
            Debug.Log("BEST MOVE: " + bestMove.Name);
            //Debug.Log("numOfPlayouts: " + numOfPlayouts);

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
                    break;
                //if (numOfPlayouts >= 10000)break;
                //Debug.Log("numOfPlayouts / maxNumOfPlayouts" + numOfPlayouts + " / " + settings.maxNumOfPlayouts);

                
                MCTSNode selectedNodeToSimulate = root;
                //1. selection
                while (selectedNodeToSimulate.visitedCount > 0)
                {
                    selectedNodeToSimulate = selectedNodeToSimulate.SelectChild();
                }

                //2. expansion
                selectedNodeToSimulate.Expand();

                // 3. simulation
                float simulationResult = selectedNodeToSimulate.Simulate(settings.playoutDepthLimit, ref numOfPlayouts);
                //numOfPlayouts++;

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