# oofem-link
Public interface for OOFEM solver consisting of command-line interface (CLI) tool and REST-ful web api interface.

## features
* file conversion engine
* geometric model, finite element mesh and fem results database management
* execution control

## how to run
Application targets .NET Core 1.1 runtime. You need to download and install .NET Core 1.1 SDK. Available [here](https://www.microsoft.com/net/download/core) (switch to Current release)

After installing dotnet, you can use dotnet tools to build and run the application from the command line. Just go to the directory containing project OofemLink.Cli and use command `dotnet run`.

To use oofem-link you must first configure the database.

## configuration
Place configuration file with name _appsettings.json_ to folder that contains application executables.

### example of appsettings.json
```
{
    "DatabaseProvider": "Sqlite",
    "ConnectionStrings": {
        "oofem_db": "Filename=C:/temp/oofem.db"
    }
}
```
Available database providers:
* SqlServer
* Sqlite
* InMemory

## CLI commands
* `list` - show list of all projects and simulations stored in OOFEM database
* `create` - create new project in OOFEM database
* `import` - import simulation data to OOFEM database
* `export` - build OOFEM input file from model in database
* `run` - run simulation in OOFEM
* `delete` - delete project from OOFEM database
* `help` - display more information on a specific command
* `version` - display version information

