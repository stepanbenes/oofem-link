# oofem-link
Public interface for OOFEM solver consisting of command-line interface (CLI) tool and REST-ful web api interface.

## features
* file conversion engine
* geometric model, finite element mesh and fem results database management
* execution control

## CLI commands
* `create` - create new project in OOFEM database
* `import` - import simulation data to OOFEM database
* `export` - build OOFEM input file from model in database
* `run` - run simulation in OOFEM
* `help` - display more information on a specific command
* `version` - display version information

## configuration
Place configuration file _appsettings.json_ to folder that contains application executables.

### Example of appsettings.json
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
