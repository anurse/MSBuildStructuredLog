using Microsoft.Build.Framework;
using Microsoft.Build.Logging.BuildDb.Model;
using Microsoft.Build.Utilities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Microsoft.Build.Logging.BuildDb
{
    public class BuildDbLogger : Logger
    {
        private BuildDbContext _db;
        private Model.Build _build;
        private Stopwatch _stopwatch;
        private SqliteConnection _connection;

        public override void Initialize(IEventSource eventSource)
        {
            _stopwatch = Stopwatch.StartNew();
            Environment.SetEnvironmentVariable("MSBUILDTARGETOUTPUTLOGGING", "true");
            Environment.SetEnvironmentVariable("MSBUILDLOGIMPORTS", "1");

            // Create the database in memory, we'll save it to disk at the end.
            // This is faster because we don't need persistence until the whole thing is built.
            // This way, individual transactions occur in memory and the whole thing is dumped to disk later.

            // We need to open a SQLite connection directly and give it to EF.
            // If we use a connection string, it'll keep closing and re-opening it, but
            // since this is an in-memory DB, it'll be recreated for every call to SaveChanges()!
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder()
                .UseSqlite(_connection)
                .Options;

            _db = new BuildDbContext(options);
            _db.Database.EnsureCreated();

            // Attach Event Handlers
            eventSource.BuildStarted += BuildStarted;
            eventSource.BuildFinished += BuildFinished;
            eventSource.ProjectStarted += ProjectStarted;
            eventSource.ProjectFinished += ProjectFinished;
        }

        public override void Shutdown()
        {
            Debug.Assert(_connection != null, "Shutdown called without calling Initialize!");

            Console.WriteLine($"Finished generating build log in {_stopwatch.Elapsed.TotalSeconds:0.00} seconds.");
            var logFile = ProcessParameters();

            // Now we "backup" the in-memory database to disk
            // Open a connection to a new on-disk DB
            using (var targetConnection = new SqliteConnection($"Data Source={logFile}"))
            {
                // Backup the data.
                var sw = new Stopwatch();
                Console.WriteLine("Saving database...");
                _connection.BackupDatabase(targetConnection);
                Console.WriteLine($"Saved in {sw.Elapsed.TotalSeconds:0.00} seconds.");
            }

            _connection.Dispose();

            base.Shutdown();
        }

        private void ProjectFinished(object sender, ProjectFinishedEventArgs args)
        {
            Debug.Assert(_build != null && _build.Id > 0, "Build must have started!");
            var project = _db.Projects.FirstOrDefault(p => p.BuildId == _build.Id && p.ProjectContextId == args.BuildEventContext.ProjectContextId);
            if (project == null)
            {
                throw new InvalidOperationException("A project that was not started finished!");
            }

            project.EndTime = args.Timestamp;
            _db.SaveChanges();
        }

        private void ProjectStarted(object sender, ProjectStartedEventArgs args)
        {
            Debug.Assert(_build != null && _build.Id > 0, "Build must have started!");
            var project = GetOrAddProject(args);

            if (args.ParentProjectBuildEventContext.ProjectContextId > 0)
            {
                var parent = GetOrAddProject(args.ParentProjectBuildEventContext.ProjectContextId);
                project.Parent = parent;
            }

            _db.SaveChanges();
        }

        private void BuildStarted(object sender, BuildStartedEventArgs e)
        {
            _build = new Model.Build()
            {
                StartTime = e.Timestamp
            };
            _db.Builds.Add(_build);

            // Create properties for each environment value
            foreach (var env in e.BuildEnvironment)
            {
                var prop = GetOrCreateProperty(env.Key, env.Value, isMetadata: false);
                var buildProperty = new BuildProperty()
                {
                    Build = _build,
                    Property = prop
                };
                _db.BuildProperties.Add(buildProperty);
                _build.Environment.Add(buildProperty);
            }

            // Save
            _db.SaveChanges();
        }

        private void BuildFinished(object sender, BuildFinishedEventArgs e)
        {
            Debug.Assert(_build != null, "Build was never started!?");

            _build.EndTime = e.Timestamp;
            _build.Succeeded = e.Succeeded;

            // TODO: Handle detailed summary?
            _db.SaveChanges();
        }

        private Project GetOrAddProject(int projectContextId)
        {
            Debug.Assert(_build != null && _build.Id > 0, "Requires a valid build.");
            var project = _db.Projects.FirstOrDefault(p => p.BuildId == _build.Id && p.ProjectContextId == projectContextId);
            if (project == null)
            {
                project = new Project()
                {
                    Build = _build,
                    ProjectContextId = projectContextId
                };
                _db.Projects.Add(project);
                _build.Projects.Add(project);
            }
            return project;
        }

        private Project GetOrAddProject(ProjectStartedEventArgs args)
        {
            var project = GetOrAddProject(args.BuildEventContext.ProjectContextId);
            UpdateProject(args, project);
            return project;
        }

        private void UpdateProject(ProjectStartedEventArgs args, Project project)
        {
            // Only update if the Targets haven't been set. That indicates that the project was originally created
            // as the parent of another project and we didn't have the definition of this project at the time.
            if (project.Targets == null)
            {
                project.StartTime = args.Timestamp;
                project.Targets = args.TargetNames;
                project.ToolsVersion = args.ToolsVersion;
                project.ProjectFile = args.ProjectFile;

                if (args.GlobalProperties != null)
                {
                    foreach (var prop in args.GlobalProperties)
                    {
                        var property = GetOrCreateProperty(prop.Key, prop.Value, isMetadata: false);
                        var projectProperty = new ProjectProperty()
                        {
                            Project = project,
                            Property = property,
                            Global = true,
                        };
                        _db.ProjectProperties.Add(projectProperty);
                        project.Properties.Add(projectProperty);
                    }
                }

                if (args.Properties != null)
                {
                    var properties = args.Properties
                        .Cast<DictionaryEntry>()
                        .OrderBy(e => e.Key)
                        .Select(d => new KeyValuePair<string, string>(Convert.ToString(d.Key), Convert.ToString(d.Value)));
                    foreach (var prop in properties)
                    {
                        var property = GetOrCreateProperty(prop.Key, prop.Value, isMetadata: false);
                        var projectProperty = new ProjectProperty()
                        {
                            Project = project,
                            Property = property,
                            Global = false,
                        };
                        _db.ProjectProperties.Add(projectProperty);
                        project.Properties.Add(projectProperty);
                    }
                }

                if (args.Items != null)
                {
                    //Finish Items
                    //var items = args.Items
                    //    .Cast<DictionaryEntry>()
                    //    .Select(d => new KeyValuePair<string, ITaskItem>(
                    //        Convert.ToString(d.Key),
                    //        d.Value as ITaskItem))
                    //    .Where(k => k.Value != null);
                    //foreach (var pair in items)
                    //{
                    //    var group = _db.ItemGroups.FirstOrDefault(g => g.Name == pair.Key);
                    //    if(group == null)
                    //    {
                    //        group = new ItemGroup()
                    //        {
                    //            Name = pair.Key
                    //        };
                    //        _db.ItemGroups.Add(group);
                    //    }

                    //    //Item could be parented to Projects or Tasks, and the metadata is associated with the instantiation.
                    //}
                }
            }
        }

        private Property GetOrCreateProperty(string name, string value, bool isMetadata)
        {
            var def = _db.PropertyDefinitions.FirstOrDefault(d => d.Name == name && d.IsMetadata == isMetadata);
            if (def == null)
            {
                def = new PropertyDefinition()
                {
                    Name = name,
                    IsMetadata = isMetadata,
                };
                _db.PropertyDefinitions.Add(def);
            }

            var prop = _db.Properties.FirstOrDefault(p => p.Definition == def && p.Value == value);
            if (prop == null)
            {
                prop = new Property
                {
                    Definition = def,
                    Value = value
                };
                _db.Properties.Add(prop);
            }
            return prop;
        }

        /// <summary>
        /// Processes the parameters given to the logger from MSBuild.
        /// </summary>
        /// <exception cref="LoggerException">
        /// </exception>
        private string ProcessParameters()
        {
            const string invalidParamSpecificationMessage = @"Need to specify a log file using the following pattern: '/logger:BuildDbLogger,BuildDbLogger.dll;log.builddb";

            if (Parameters == null)
            {
                throw new LoggerException(invalidParamSpecificationMessage);
            }

            string[] parameters = Parameters.Split(';');

            if (parameters.Length != 1)
            {
                throw new LoggerException(invalidParamSpecificationMessage);
            }

            return parameters[0].TrimStart('"').TrimEnd('"');
        }
    }
}
