using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnityExplorer.UI.Widgets
{
    public interface IDynamicDataSource : IPoolDataSource
    {
        //float GetHeightForCell(int index);
        float DefaultCellHeight { get; }
    }
}
