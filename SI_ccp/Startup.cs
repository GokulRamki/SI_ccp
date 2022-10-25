using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(SI_ccp.Startup))]
namespace SI_ccp
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
