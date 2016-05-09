#pragma once
#include "Graph.h"
#include <set>

/*
    Meta data used by the solver function to keep track of traversal
*/
struct Meta
{
    Meta()
        : node(UINT_MAX), // The node's ID for this metadata (see Graph.h)
          prev(UINT_MAX), // Previous node, for pathing
        // Bookkeeping data
          g(FLT_MAX),   // Actual Cost
          h(0),         // Guessed Cost
          f(g),         // Combined Cost
          DOS(0),       // Degrees of Separation
          state(STATE::undiscovered) // State, for frontier management
    {}
   
    size_t node, prev;
    float g, h, f;
    unsigned DOS;

    enum class STATE { undiscovered, frontier, explored } state;
};


// Some provided comparison functions
inline bool Dijkstra     (const Meta *a, const Meta *b) { return a->g   < b->g;   }
inline bool Greedy       (const Meta *a, const Meta *b) { return a->h   < b->h;   }
inline bool AStar        (const Meta *a, const Meta *b) { return a->f   < b->f;   }
inline bool DepthFirst   (const Meta *a, const Meta *b) { return a->DOS > b->DOS; }
inline bool BreadthFirst (const Meta *a, const Meta *b) { return a->DOS < b->DOS; }

/*
    Start and Goal are provided as values instead of node IDs.

    In a continuous environment, are 'movement' isn't locked to the value in the nodes.
    This allows us to 'search' for the closest nodes to the provided start and goal.

    Search Range indicates how far we'll search from the provided value.
        For discrete environments, setting it to 0 is fine.

    Fudge Equals is used to determine whether or not the solution path will included
        the start and goal values. If the distance from the first node and start value is
        greater than the fudge, the path will include the start. And vice versa for the goal.
*/
template<typename T>
std::list<T> FindPath(const Graph<T> &graph, const T &start, const T &goal,
        float(*Heuristic )(const T&, const T&),
        bool (*Comparison)(const Meta *, const Meta *), float searchRange = 0.0001f, float fudgeEquals = 0.0001f)
{
    //setup data
    std::list<T> path;
    std::list<Meta*> frontier;
    std::map<size_t, Meta> metadata;

    // find indices for the starting nodes
    int startNode = graph.findNode(start, Heuristic, searchRange);
    int goalNode  = graph.findNode(goal , Heuristic, searchRange);
    
    assert(startNode >= 0 && goalNode >= 0);

    Meta *current = nullptr;
    bool solved   = false;

    ////////////////////////////////////////////////////////////////////
    //// Initialize the starting node's metadata and put it into the frontier
    Meta ts;
    ts.node = startNode;
    ts.prev = startNode;
    ts.g = 0;
    ts.h = Heuristic(graph.getNodeData(startNode), goal);
    ts.f = ts.h;
    ts.state = Meta::STATE::frontier;
    ts.DOS = 0;  
    metadata[startNode] = ts;
    frontier.push_back(&metadata[startNode]);


    ////////////////////////////////////////////////////////////////////
    // Begin the algorithm
    while (!frontier.empty())
    {
        // Get node off of frontier
        frontier.sort(Comparison);
        current = frontier.front();
        frontier.pop_front();
        
        // victory!
        if (solved = current->node == goalNode) break;

        // A node is explored if all its neighbors are 
        // in the frontier
        current->state = Meta::STATE::explored;


        // evaluate metadata and populate the frontier
        for each(auto eid in graph.getNodeEdges(current->node))       
            if(metadata[graph.getEdgeData(eid).next].state != Meta::STATE::explored)
            {
                // accumulate new meta data for the node
                auto edge = graph.getEdgeData(eid);
                Meta t;
                t.prev  = edge.prev;
                t.node  = edge.next;
                t.g     = edge.cost + current->g;
                t.h     = Heuristic(graph.getNodeData(t.node), goal);
                t.f     = t.g + t.h;
                t.DOS   = current->DOS + 1;
                t.state = Meta::STATE::frontier;

                // push into frontier if necessary
                if (metadata[t.node].state == Meta::STATE::undiscovered)                
                    frontier.push_back(&metadata[t.node]);
                
                // update bookkeeping if better path found
                if (t.g < metadata[t.node].g)                
                    metadata[t.node] = t;                
        }
    }

    if (!solved) return path; // Path is empty.

    ////////////////////////////////////////////////////////////////////
    //// Evaluate the path by backtracking from the goal

    // check to see if the goal node is close enough to the goal value   
    if(Heuristic(goal, graph.getNodeData(current->node)) > fudgeEquals)
        path.push_front(goal); // if it isn't, push the goal value into the path

    for (; current->node != startNode; current = &metadata[current->prev])
        path.push_front(graph.getNodeData(current->node));
   

    path.push_front(graph.getNodeData(current->node));
    
    if (Heuristic(start, graph.getNodeData(current->node)) > fudgeEquals)
        path.push_front(start);

    return path;
}

