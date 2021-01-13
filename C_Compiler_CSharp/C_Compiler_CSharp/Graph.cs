using System.Collections.Generic;

namespace CCompiler {
  public class Edge<VertexType> {
    private VertexType m_vertex1, m_vertex2;

    public Edge(VertexType vertex1, VertexType vertex2) {
      m_vertex1 = vertex1;
      m_vertex2 = vertex2;
    }
  }

  public class Graph<VertexType> {
    private ISet<VertexType> m_vertexSet;
    private ISet<Pair<VertexType,VertexType>> m_edgeSet;

    public Graph() {
      m_vertexSet = new HashSet<VertexType>();
      m_edgeSet = new HashSet<Pair<VertexType,VertexType>>();
    }
  
    public Graph(ISet<VertexType> vertexSet) {
      m_vertexSet = vertexSet;
      m_edgeSet = new HashSet<Pair<VertexType,VertexType>>();
    }

    public Graph(ISet<VertexType> vertexSet,
                 ISet<Pair<VertexType,VertexType>> edgeSet) {
      m_vertexSet = vertexSet;
      m_edgeSet = edgeSet;
    }

    public ISet<VertexType> VertexSet {
      get { return m_vertexSet; }
    }
  
    public ISet<Pair<VertexType,VertexType>> EdgeSet {
      get { return m_edgeSet; }
    }

    public ISet<VertexType> GetNeighbourSet(VertexType vertex) {
      ISet<VertexType> neighbourSet = new HashSet<VertexType>();
    
      foreach (Pair<VertexType,VertexType> edge in m_edgeSet) {
        if (edge.First.Equals(vertex)) {
          neighbourSet.Add(edge.Second);
        }
      
        if (edge.Second.Equals(vertex)) {
          neighbourSet.Add(edge.First);
        }      
      }
    
      return neighbourSet;
    }

    public void AddVertex(VertexType vertex) {
      m_vertexSet.Add(vertex);
    }
 
    public void EraseVertex(VertexType vertex) {
      ISet<Pair<VertexType,VertexType>> edgeSetCopy =
        new HashSet<Pair<VertexType,VertexType>>(m_edgeSet);

      foreach (Pair<VertexType,VertexType> edge in edgeSetCopy) {
        if ((vertex.Equals(edge.First)) || (vertex.Equals(edge.Second))) {
          m_edgeSet.Remove(edge);
        }
      }

      m_vertexSet.Remove(vertex);
    }

    public void AddEdge(VertexType vertex1, VertexType vertex2) {
      Pair<VertexType,VertexType> edge =
        new Pair<VertexType,VertexType>(vertex1, vertex2);
      m_edgeSet.Add(edge);
    }

    public void EraseEdge(VertexType vertex1, VertexType vertex2) {
      Pair<VertexType,VertexType> edge =
        new Pair<VertexType,VertexType>(vertex1, vertex2);
      m_edgeSet.Remove(edge);
    }

    public ISet<Graph<VertexType>> Split() {
      ISet<ISet<VertexType>> subgraphSet = new HashSet<ISet<VertexType>>();

      foreach (VertexType vertex in m_vertexSet) {
        ISet<VertexType> vertexSet = new HashSet<VertexType>();
        DeepSearch(vertex, vertexSet);
        subgraphSet.Add(vertexSet);
      }

      ISet<Graph<VertexType>> graphSet = new HashSet<Graph<VertexType>>();
      foreach (ISet<VertexType> vertexSet in subgraphSet) {
        Graph<VertexType> subGraph = InducedSubGraph(vertexSet);
        graphSet.Add(subGraph);
      }

      return graphSet;
    }

    private void DeepSearch(VertexType vertex, ISet<VertexType> resultSet) {
      if (!resultSet.Contains(vertex)) {
        resultSet.Add(vertex);
        ISet<VertexType> neighbourSet = GetNeighbourSet(vertex);
        
        foreach (VertexType neighbour in neighbourSet) {
          DeepSearch(neighbour, resultSet);
        }
      }
    }

    private Graph<VertexType> InducedSubGraph(ISet<VertexType> vertexSet) {
      ISet<Pair<VertexType,VertexType>> resultEdgeSet = new HashSet<Pair<VertexType,VertexType>>();
   
      foreach (Pair<VertexType,VertexType> edge in m_edgeSet) {
        if (vertexSet.Contains(edge.First) &&
            vertexSet.Contains(edge.Second)) {
          resultEdgeSet.Add(edge);
        }
      }
   
      return (new Graph<VertexType>(vertexSet, resultEdgeSet));
    } 
  }
}