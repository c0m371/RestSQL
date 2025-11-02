using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestSQL.IntegrationTests.SqlServer
{
    public class SqlServerTests : PostsTestsBase<SqlServerFixture>
    {
        public SqlServerTests(SqlServerFixture fixture) : base(fixture) { }

        protected override string YamlFolder => Path.Combine(AppContext.BaseDirectory, "SqlServer", "Yaml");
    }
}
