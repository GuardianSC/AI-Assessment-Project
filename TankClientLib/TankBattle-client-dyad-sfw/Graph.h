#pragma once
#include <cassert>
#include <list>
#include <map>
#include <algorithm>

/*
    Templated Graph class designed to work with a generic solver function.
    If T is POD, then it is copy-safe.
*/

template<typename T>
class Graph
{
public:    
    struct Edge
    {
        size_t self;
        size_t prev, next;
        float cost;
    };

    struct Node
    {
        size_t self;
        T data;
        std::list<size_t> edges, _incoming; // keeping track of incoming edges makes removal a little simpler
    };
private:
    // All of the data is stored internally, IDs, rather than pointers, are used to keep track of each node.
    std::map<size_t, Node> nodes;
    std::map<size_t, Edge> edges;
    
    size_t nextNodeIDX = 0;
    size_t nextEdgeIDX = 0;


public:
    auto &getNodes() const { return nodes; }
    auto &getEdges() const { return edges; }

    size_t addNode(const T &data) { nodes.emplace(nextNodeIDX, Node{nextNodeIDX, data}); return nextNodeIDX++; }
    void   addEdge(size_t prev, size_t next, float cost = 1)
    {
        addDirectedEdge(prev, next, cost);
        addDirectedEdge(next, prev, cost);
    }

    size_t addDirectedEdge(size_t prev, size_t next, float cost = 1)
    {
        assert(nodes.count(prev) && nodes.count(next) && " Node ID invalid");

        edges.emplace(nextEdgeIDX, Edge{nextEdgeIDX, prev, next, cost});
        nodes[prev].edges.push_back(nextEdgeIDX);
        nodes[next]._incoming.push_back(nextEdgeIDX);
        return nextEdgeIDX++;
    }


    void   removeNode(size_t node)
    {
        assert(nodes.count(node) && " Node ID invalid.");
        
        std::list<size_t> edge_del; // queue for edges to delete

        for each(auto e in nodes[node].edges)
        {
            edge_del.push_back(e);
            nodes[edges[e].next]._incoming.remove(e);
        }

        for each(auto e in nodes[node]._incoming)
        {
            edge_del.push_back(e);
            nodes[edges[e].prev].edges.remove(e);
        }
        
        for each(auto e in edge_del) edges.erase(e);

        nodes.erase(node);
    }
    

    // Attempts to find a node whose value is close to the query
    // Since this is templated, we don't know how to compare the unknown type.
    // A function pointer needs to be provided that will be used to determine the 'distance'
    // from the query to each of the values in the nodes.
    // If the distance to the closest node is greater than the range, returns -1.
    int findNode(const T& query, float (*Distance)(const T&, const T&), float range = 0.0001f)  const
    {
        float sd = FLT_MAX;
        int   id = -1;

        for each(auto np in nodes)
        {
            float d = Distance(np.second.data, query);

            if (d < sd && d <= range)
            {                
                sd = d;
                id = np.second.self;
            }
        }   
        return id;
    }

    Node &NodeAt(size_t node) { return nodes.at(node); }
    Edge &EdgeAt(size_t edge) { return edges.at(edge); }

    T getNodeData(size_t node) const { return nodes.at(node).data; }
    Edge getEdgeData(size_t edge) const { return edges.at(edge); }
    std::list<size_t> getNodeEdges(size_t node) const { return nodes.at(node).edges; }
};