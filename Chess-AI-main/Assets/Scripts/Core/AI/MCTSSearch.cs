﻿namespace Chess
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using UnityEngine;
    using static System.Math;
    using System.Linq; // Add this to access LINQ methods

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
            root = new MCTSNode(board, moveGenerator, Move.InvalidMove, true);
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

            SearchMoves();

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
                //1. selection
                MCTSNode selectedNodeToExpand = root;
                while (selectedNodeToExpand.children.Count > 0)
                {
                    selectedNodeToExpand = selectedNodeToExpand.SelectChild();
                }

                //2. expansion
                MCTSNode expandedNode = selectedNodeToExpand.Expand();
                if (expandedNode != null)
                {
                    // 3. simulation
                    float simulationResult = expandedNode.Simulate(settings.playoutDepthLimit);

                    // 4. backpropagation
                    expandedNode.Backpropagate(simulationResult);

                    bestMove = root.children.OrderByDescending(child => child.UCTValue).FirstOrDefault().initialMove;
                }
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