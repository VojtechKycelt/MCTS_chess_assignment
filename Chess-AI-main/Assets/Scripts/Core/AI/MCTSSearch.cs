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
            
            SearchMoves();

            bestMove = root.children
                .Select((child, index) => new { child, index }) // Attach original index
                .OrderByDescending(x => x.child.rewards)        // Sort by rewards
                .ThenBy(x => x.index)                           // Maintain original order for ties
                .FirstOrDefault().child.initialMove;
            //bestMove = root.children.OrderByDescending(child => child.rewards).FirstOrDefault().initialMove;

            onSearchComplete?.Invoke(bestMove);

            //DEBUG
            /*Debug.Log("-------------------CHILDREN-------------------------");
            foreach (MCTSNode node in root.children)//.OrderBy(child => child.rewards))
            {
                Debug.Log("move: " + node.initialMove.Name + ", visitedCount: " + node.visitedCount + ", rewards: " + node.rewards + ", UCT: " + node.UCTValue.ToString());

            }
            Debug.Log("BEST MOVE: " + bestMove.Name);
            Debug.Log("numOfPlayouts: " + numOfPlayouts);//*/
             
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
                {
                    abortSearch = true;
                    break;
                }
                    
                
                MCTSNode selectedNodeToSimulate = root;

                //1. selection
                while (selectedNodeToSimulate.visitedCount > 0 && selectedNodeToSimulate.unexploredMoves.Count == 0)
                    selectedNodeToSimulate = selectedNodeToSimulate.SelectChild();
                
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