#pragma once
#include "Graph.h"
#include "Solver.h"
#include "Vector2.h"

/*
    This distance function will be used as a heuristic.
        Manhattan distance that prioritizes not changing directions.
*/
inline float Brooklyn(const Vector2 &a, const Vector2 &b)
{
    float xDiff = fabsf(a.x - b.x);
    float yDiff = fabsf(a.y - b.y);
    
    return xDiff < yDiff ? xDiff : yDiff;
}

class Grid
{
    Graph<Vector2> map;
    const size_t rows, cols;
public:
    Grid(const Vector2 &start = Vector2{ -50, -50 }, const Vector2 &offset = Vector2{ 10, 10 }, size_t rows = 10, size_t cols = 10) : rows(rows), cols(cols)
    {          
        for (unsigned j = 0; j < cols; ++j)
            for (unsigned i = 0; i < rows; ++i)
            {
                Vector2 pos = start + Vector2{ (float)i, (float)j } * offset;
                size_t id = map.addNode(pos);                
            }

        for (unsigned i = 0; i < cols * rows; ++i)
        {
            if((i%rows +1)    < cols     ) map.addEdge(i, i +    1, 10);
            if(i+rows         < cols*rows) map.addEdge(i, i + rows, 10);
        }
    }

    std::list<Vector2> getPath(const Vector2 &a, const Vector2 &b)
    {
        return FindPath<Vector2>(map, a, b, Brooklyn, AStar, 20, 10);
    }    

    // Find the closest node to the argument vector and set its edge weights to weight
    void addObstacle(const Vector2 &a, float weight = FLT_MAX)
    {
        size_t n = map.findNode(a, distance, 10);
        
        size_t len = map.getNodeEdges(n).size();
        
        std::list<size_t> edges;
        
        edges.insert(edges.begin(), map.NodeAt(n).edges.begin(), map.NodeAt(n).edges.end());
        edges.insert(edges.begin(), map.NodeAt(n)._incoming.begin(), map.NodeAt(n)._incoming.end());

        for each(size_t e in edges)
            map.EdgeAt(e).cost = weight;
    }
};







//
//
//class Grid
//{
//    Graph<Vector2> map;
//    std::map<size_t, float> heat;
//    const size_t rows, cols;
//public:
//    void update(float dt)
//    {
//        for (unsigned i = 0; i < heat.size(); ++i)
//        {
//            heat[i] += dt;
//        }
//    }
//
//
//    void refreshAll(const Vector2 &pos, const Vector2 &dir, float halfcosfov = .717, float range = 30)
//    {
//        for each(auto it in map.getNodes())
//        {
//            if (distance(pos, it.second.data) < range)
//                heat[it.first] = 0;
//        }
//    }
//
//    Vector2 getCoolest()
//    {
//        size_t max = 0;
//        float temp = 0;
//
//        for (unsigned i = 0; i < heat.size(); ++i)
//        {
//            if (heat[i] > temp)
//            {
//                max = i;
//                temp = heat[i];
//            }
//        }
//
//        return map.getNodeData(max);
//    }
//
//
//    Grid(const Vector2 &start = Vector2{ -50, -50 }, const Vector2 &offset = Vector2{ 10, 10 }, size_t rows = 10, size_t cols = 10) : rows(rows), cols(cols)
//    {
//        for (unsigned j = 0; j < cols; ++j)
//            for (unsigned i = 0; i < rows; ++i)
//            {
//                Vector2 pos = start + Vector2{ (float)i,(float)j } *offset;
//                size_t id = map.addNode(pos);
//                heat[id] = 0;
//            }
//
//        for (unsigned i = 0; i < cols * rows; ++i)
//        {
//            if ((i%rows + 1)    < cols) map.addEdge(i, i + 1, 10);
//            if (i + rows         < cols*rows) map.addEdge(i, i + rows, 10);
//        }
//    }
//
//    std::list<Vector2> getPath(const Vector2 &a, const Vector2 &b)
//    {
//        return FindPath<Vector2>(map, a, b, Brooklyn, AStar, 20, 10);
//    }
//
//    void addObstacle(const Vector2 &a);
//};