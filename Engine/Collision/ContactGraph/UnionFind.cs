using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Collision.ContactGraph
{
    public class UnionFind
    {

        //Dictionary<int, int> _rank = [];
        Dictionary<int, int> _parent = [];

        public UnionFind()
        {
        }

        public void MakeSet(int i)
        {
            _parent[i] = i;
            //_rank[i] = 0;
        }

        public int Find(int i)
        {
            if(i<0) return -1;
            if (_parent[i] != i)
                _parent[i] = Find(_parent[i]);
            return _parent[i];
        }

        public void Union(int i, int j)
        {
            int rootI = Find(i);
            int rootJ = Find(j);
            if (rootI != rootJ)
            {
                _parent[rootJ] = rootI;
                return;


                //if (_rank[rootI] < _rank[rootJ])
                //{
                //    _parent[rootI] = rootJ;
                //}
                //else if (_rank[rootI] > _rank[rootJ])
                //{
                //    _parent[rootJ] = rootI;
                //}
                //else
                //{
                //    _parent[rootJ] = rootI;
                //    _rank[rootI]++;
                //}
            }
        }
    }
}
