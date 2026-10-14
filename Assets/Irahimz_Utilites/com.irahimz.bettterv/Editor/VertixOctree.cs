using System.Collections.Generic;
using UnityEngine;

public class VertexOctree
    {
    private class OctreeNode
        {
        public Bounds bounds;
        public List<Vector3> vertices;
        public OctreeNode[] children;

        public OctreeNode(Bounds b)
            {
            bounds = b;
            vertices = new List<Vector3>();
            }
        }

    private OctreeNode rootNode;
    private const int MaxVerticesPerNode = 32;
    private const int MaxDepth = 8;

    public VertexOctree(MeshFilter[] meshFilters)
        {
        if (meshFilters == null || meshFilters.Length == 0) return;

        // Step 1: Find the absolute bounds of ALL children combined
        Bounds masterBounds = new Bounds();
        bool boundsInitialized = false;

        foreach (MeshFilter filter in meshFilters)
            {
            if (filter.sharedMesh == null) continue;

            Transform objTransform = filter.transform;
            Vector3[] localVertices = filter.sharedMesh.vertices;

            foreach (Vector3 localVertex in localVertices)
                {
                Vector3 worldVertex = objTransform.TransformPoint(localVertex);
                if (!boundsInitialized)
                    {
                    masterBounds = new Bounds(worldVertex, Vector3.zero);
                    boundsInitialized = true;
                    }
                else
                    {
                    masterBounds.Encapsulate(worldVertex);
                    }
                }
            }

        if (!boundsInitialized) return;

        masterBounds.Expand(0.01f);
        rootNode = new OctreeNode(masterBounds);

        // Step 2: Insert every vertex from every child into the master Octree
        foreach (MeshFilter filter in meshFilters)
            {
            if (filter.sharedMesh == null) continue;

            Transform objTransform = filter.transform;
            Vector3[] localVertices = filter.sharedMesh.vertices;

            foreach (Vector3 localVertex in localVertices)
                {
                Vector3 worldVertex = objTransform.TransformPoint(localVertex);
                InsertVertex(rootNode, worldVertex, 0);
                }
            }
        }

    private void InsertVertex(OctreeNode node, Vector3 vertex, int currentDepth)
        {
        if (node.children == null)
            {
            node.vertices.Add(vertex);

            if (node.vertices.Count > MaxVerticesPerNode && currentDepth < MaxDepth)
                {
                SplitNode(node);
                List<Vector3> oldVertices = node.vertices;
                node.vertices = null;

                foreach (Vector3 v in oldVertices)
                    {
                    InsertIntoChildren(node, v, currentDepth + 1);
                    }
                }
            }
        else
            {
            InsertIntoChildren(node, vertex, currentDepth + 1);
            }
        }

    private void SplitNode(OctreeNode node)
        {
        node.children = new OctreeNode[8];
        Vector3 size = node.bounds.size / 2f;
        Vector3 center = node.bounds.center;
        Vector3 extents = size / 2f;

        int index = 0;
        for (int x = -1; x <= 1; x += 2)
            {
            for (int y = -1; y <= 1; y += 2)
                {
                for (int z = -1; z <= 1; z += 2)
                    {
                    Vector3 childCenter = center + new Vector3(extents.x * x, extents.y * y, extents.z * z);
                    node.children[index++] = new OctreeNode(new Bounds(childCenter, size));
                    }
                }
            }
        }

    private void InsertIntoChildren(OctreeNode node, Vector3 vertex, int nextDepth)
        {
        foreach (OctreeNode child in node.children)
            {
            if (child.bounds.Contains(vertex))
                {
                InsertVertex(child, vertex, nextDepth);
                return;
                }
            }
        }

    public bool GetClosestVertex(Ray ray, out Vector3 closestVertex, out float minDistance)
        {
        closestVertex = Vector3.zero;
        minDistance = float.MaxValue;

        if (rootNode == null || !rootNode.bounds.IntersectRay(ray))
            {
            return false;
            }

        FindClosestInNode(rootNode, ray, ref closestVertex, ref minDistance);
        return minDistance != float.MaxValue;
        }

    private void FindClosestInNode(OctreeNode node, Ray ray, ref Vector3 bestVertex, ref float bestDist)
        {
        if (!node.bounds.IntersectRay(ray)) return;

        if (node.children != null)
            {
            foreach (OctreeNode child in node.children)
                {
                FindClosestInNode(child, ray, ref bestVertex, ref bestDist);
                }
            }
        else if (node.vertices != null)
            {
            foreach (Vector3 vertex in node.vertices)
                {
                Vector3 offset = vertex - ray.origin;
                float distanceToRay = Vector3.Cross(ray.direction, offset).magnitude;

                if (distanceToRay < bestDist)
                    {
                    bestDist = distanceToRay;
                    bestVertex = vertex;
                    }
                }
            }
        }
    }