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
using CareerCracker.Models;
using Microsoft.Extensions.Options;

namespace CareerCracker.DataBaseLayer
{
    public partial class DataBaseLayer : IDataBaseLayer
    {
        private readonly RazorpaySettings _rz;
        private readonly IConfiguration _configuration;
        private readonly string DbConnection;

        public DataBaseLayer(IOptions<RazorpaySettings> rz, IConfiguration configuration)
        {
            _rz = rz.Value;
            _configuration = configuration;
            DbConnection = _configuration.GetConnectionString("AppDbContextConnection");
        }
    }
}
