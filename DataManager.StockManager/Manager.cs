namespace DataManager.StockManager
{
    public class Manager(API.Function function = API.Function.TIME_SERIES_DAILY)
    {
        public List<string> Symbols { get; init; } = API.APIHandler.AvailabelSymbols();
        public DataManager.API.Function Function { get; private set; } = function;
        public List<Data.DataPoints> DataPoints { get; private set; } = [];

        public async Task LoadData()
        {
            if (Symbols.Count == 0)
            {
                throw new Exception("No symbols to load data for.");
            }
            foreach (var symbol in Symbols)
            {
                var data = await API.APIHandler.GetDataSet(symbol, Function);
                if (data != null && data.Size() != 0)
                {
                    DataPoints.Add(data);
                }
            }
        }

        public void ClearData()
        {
            DataPoints.Clear();
        }
        public void ReLoadData(API.Function function)
        {
            Function = function;
            ClearData();
            LoadData().Wait();
        }
        public List<string> GetNames()
        {
            return [.. DataPoints.Select(dp => dp.Name)];
        }
        public List<(string Name, double Value)> GetLastValues()
        {
            return [.. DataPoints.Select(dp => (dp.Name, dp.Data.Last().Value))];
        }
        public List<(string Name, double Value)> GetMinValues()
        {
            return [.. DataPoints.Select(dp => (dp.Name, dp.MinValue))];
        }
        public List<(string Name, double Value)> GetMaxValues()
        {
            return [.. DataPoints.Select(dp => (dp.Name, dp.MaxValue))];
        }



    }
}
