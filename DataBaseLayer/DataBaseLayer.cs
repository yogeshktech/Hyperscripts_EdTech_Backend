//namespace CareerCracker.DataBaseLayer
//{
//    public partial interface IDataBaseLayer
//    {
//    }

//    public partial class DataBaseLayer : IDataBaseLayer
//    {
//        private readonly IConfiguration _configuration;
//        private readonly string DbConnection;
//        public DataBaseLayer(IConfiguration configuration)
//        {
//            this._configuration = configuration;
//            this.DbConnection = this._configuration.GetConnectionString("AppDbContextConnection");
//        }
//    }
//}
namespace CareerCracker.DataBaseLayer
{
    public partial class DataBaseLayer : IDataBaseLayer
    {
        private readonly IConfiguration _configuration;
        private readonly string DbConnection;

        public DataBaseLayer(IConfiguration configuration)
        {
            _configuration = configuration;
            DbConnection = _configuration.GetConnectionString("AppDbContextConnection");
        }
    }
}
