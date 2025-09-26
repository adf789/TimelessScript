
using System.Collections.Generic;

public class TableSubManager : SubBaseManager<TableSubManager>
{
    private Dictionary<System.Type, BaseTable> tables = new Dictionary<System.Type, BaseTable>();

    public T Get<T>() where T : BaseTable
    {
        if (tables.TryGetValue(typeof(T), out var table))
            return table as T;

        var resourcePath = ResourcesTypeRegistry.Get().GetResourcesPath<BaseTable>();
        table = resourcePath.LoadByName<BaseTable>(typeof(T).Name);

        tables[typeof(T)] = table;

        return table as T;
    }
}
