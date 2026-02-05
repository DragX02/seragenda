using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Dapper;
using seragenda.Models;

namespace seragenda.Controllers
{

    [Route("api/[controller")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private readonly IConfiguration _config;
        public ValuesController(IConfiguration config)
        {
            _config = config;
        }
        [HttpGet]
        public IActionResult GetCours()
        {
            var ConString = _config.GetConnectionString("DefaultConnection");
            using (var connection = new NpgsqlConnection(ConString))
            {
                var sql = " SELECT * FROM cours";
                var maList = connection.Query<Cours>(sql).ToList();
                return Ok(maList);
            }
        }
    }


}   