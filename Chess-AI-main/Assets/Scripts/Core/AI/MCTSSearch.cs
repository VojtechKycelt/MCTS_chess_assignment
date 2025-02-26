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
    using UnityEditor;

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

        // Diagnostics
        public SearchDiagnostics Diagnostics { get; set; }
        System.Diagnostics.Stopwatch searchStopwatch;

        //My added variables
        MCTSNode root;
        bool team;
        int numOfPlayouts;

        public MCTSSearch(Board board, MCTSSettings settings)
        {
            this.board = board;
            this.settings = settings;
            evaluation = new Evaluation();
            moveGenerator = new MoveGenerator();
            rand = new System.Random();

            team = board.WhiteToMove;
            root = new MCTSNode(board, moveGenerator, rand, evaluation, Move.InvalidMove, true, team);
        }

        public void StartSearch()
        {
            InitDebugInfo();

            // Initialize search settings
            bestEval = 0;
            bestMove = Move.InvalidMove;

            moveGenerator.promotionsToGenerate = settings.promotionsToSearch;
            abortSearch = false;
            Diagnostics = new SearchDiagnostics();

            team = board.WhiteToMove;
            numOfPlayouts = 0;
            root = new MCTSNode(board, moveGenerator, rand, evaluation, Move.InvalidMove, true, team);
            root.Expand();

            SearchMoves();

            bestMove = root.children
                .Select((child, index) => new { child, index }) // Attach original index
                .OrderByDescending(x => (x.child.rewards/x.child.visitedCount)) // Sort by average reward
                .ThenBy(x => x.index) // Maintain original order for ties
                .FirstOrDefault().child.initialMove;


            onSearchComplete?.Invoke(bestMove);

            if (!settings.useThreading)
            {
                //LogDebugInfo();
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
                {
                    abortSearch = true;
                    break;
                }

                MCTSNode selectedNodeToSimulate = root;

                //1. selection
                while (selectedNodeToSimulate.children.Count > 0)
                {
                    selectedNodeToSimulate = selectedNodeToSimulate.SelectChild();
                }

                //2. expansion
                selectedNodeToSimulate = selectedNodeToSimulate.Expand();

                // 3. simulation
                float simulationResult = selectedNodeToSimulate.Simulate(settings.playoutDepthLimit);
                numOfPlayouts++;
                if (selectedNodeToSimulate.isMyTurn)
                    simulationResult = 1 - simulationResult;
                // 4. backpropagation
                selectedNodeToSimulate.Backpropagate(1 - simulationResult); 
            }
        }

        void LogDebugInfo()
        {
            // Optional
            Debug.Log("-------------------UNEXPLORED MOVES-------------------------");

            foreach (Move move in moveGenerator.GenerateMoves(board, true))
            {
                Debug.Log("move: " + move.Name);

            };

            //DEBUG
            Debug.Log("-------------------CHILDREN-------------------------");
            foreach (MCTSNode node in root.children.OrderBy(child => child.rewards))
            {
                Debug.Log("move: " + node.initialMove.Name + ", visitedCount: " + node.visitedCount + ", rewards: " + node.rewards + ", UCT: " + node.UCTValue.ToString());

            }
            Debug.Log("BEST MOVE: " + bestMove.Name);
            Debug.Log("numOfPlayouts: " + numOfPlayouts);//*/
        }

        void InitDebugInfo()
        {
            searchStopwatch = System.Diagnostics.Stopwatch.StartNew();
            // Optional
        }
    }
}