using System.Text;
using System.Collections.Generic;

namespace CCompiler {
  public class Graph<VertexType> {
    private ISet<VertexType> m_vertexSet;
    private ISet<UnorderedPair<VertexType,VertexType>> m_edgeSet;

    public Graph() {
      m_vertexSet = new HashSet<VertexType>();
      m_edgeSet = new HashSet<UnorderedPair<VertexType,VertexType>>();
    }
  
    public Graph(ISet<VertexType> vertexSet) {
      m_vertexSet = vertexSet;
      m_edgeSet = new HashSet<UnorderedPair<VertexType,VertexType>>();
    }

    public Graph(ISet<VertexType> vertexSet,
                 ISet<UnorderedPair<VertexType,VertexType>> edgeSet) {
      m_vertexSet = vertexSet;
      m_edgeSet = edgeSet;
    }

    public ISet<VertexType> VertexSet {
      get { return m_vertexSet; }
    }
  
    public ISet<UnorderedPair<VertexType,VertexType>> EdgeSet {
      get { return m_edgeSet; }
    }

    public ISet<VertexType> GetNeighbourSet(VertexType vertex) {
      ISet<VertexType> neighbourSet = new HashSet<VertexType>();
    
      foreach (UnorderedPair<VertexType,VertexType> edge in m_edgeSet) {
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
      m_vertexSet.Remove(vertex);
    }
  
    public void AddEdge(VertexType vertex1, VertexType vertex2) {
      m_edgeSet.Add(new UnorderedPair<VertexType,VertexType>(vertex1, vertex2));
    }
  
    public void AddEdge(UnorderedPair<VertexType,VertexType> edge) {
      m_edgeSet.Add(edge);
    }
  
    public void EraseEdge(VertexType vertex1, VertexType vertex2) {
      m_edgeSet.Remove(new UnorderedPair<VertexType,VertexType>(vertex1, vertex2));
    }

    public void EraseEdge(UnorderedPair<VertexType,VertexType> edge) {
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
        graphSet.Add(InducedSubGraph(vertexSet));
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
      ISet<UnorderedPair<VertexType,VertexType>> resultEdgeSet = new HashSet<UnorderedPair<VertexType,VertexType>>();
   
      foreach (UnorderedPair<VertexType,VertexType> edge in m_edgeSet) {
        if (vertexSet.Contains(edge.First) &&
            vertexSet.Contains(edge.Second)) {
          resultEdgeSet.Add(edge);
        }
      }
   
      return (new Graph<VertexType>(vertexSet, resultEdgeSet));
    } 
  }
}