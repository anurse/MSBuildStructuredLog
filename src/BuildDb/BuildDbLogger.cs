using Microsoft.Build.Framework;
using Microsoft.Build.Logging.BuildDb.Model;
using Microsoft.Build.Utilities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Microsoft.Build.Logging.BuildDb
{
    public class BuildDbLogger : Logger
    {
        private BuildDbContext _db;
        private Model.Build _build;

        public override void Initialize(IEventSource eventSource)
        {
            Environment.SetEnvironmentVariable("MSBUILDTARGETOUTPUTLOGGING", "true");
            Environment.SetEnvironmentVariable("MSBUILDLOGIMPORTS", "1");

            var logFile = ProcessParameters();

            // Create the database
            var options = new DbContextOptionsBuilder()
                .UseSqlite($"Data Source={Path.GetFullPath(logFile)}")
                .Options;

            _db = new BuildDbContext(options);
            _db.Database.EnsureDeleted();
            _db.Database.Migrate();

            // Attach Event Handlers
            eventSource.BuildStarted += BuildStarted;
            eventSource.BuildFinished += BuildFinished;
            eventSource.ProjectStarted += ProjectStarted;
            eventSource.ProjectFinished += ProjectFinished;
        }

        private void ProjectFinished(object sender, ProjectFinishedEventArgs args)
        {
            Debug.Assert(_build != null && _build.Id > 0, "Build must have started!");
            var project = _db.Projects.FirstOrDefault(p => p.BuildId == _build.Id && p.ProjectContextId == args.BuildEventContext.ProjectContextId);
            if(project == null)
            {
                throw new InvalidOperationException("A project that was not started finished!");
            }

            project.EndTime = args.Timestamp;
            _db.SaveChanges();
        }

        private void ProjectStarted(object sender, ProjectStartedEventArgs args)
        {
            Debug.Assert(_build != null && _build.Id > 0, "Build must have started!");
            var project = _db.Projects.FirstOrDefault(p => p.BuildId == _build.Id && p.ProjectContextId == args.BuildEventContext.ProjectContextId);
            if (project == null)
            {
                project = new Project()
                {
                    Build = _build,
                    ProjectContextId = args.BuildEventContext.ProjectContextId
                };
                _db.Projects.Add(project);
                _build.Projects.Add(project);
            }

            project.StartTime = args.Timestamp;
            project.Name = args.Message;
            project.ProjectFile = args.ProjectFile;

            if (args.GlobalProperties != null)
            {
                foreach (var prop in args.GlobalProperties)
                {
                    var property = GetOrCreateProperty(prop.Key, prop.Value);
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
                    var property = GetOrCreateProperty(prop.Key, prop.Value);
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
                // TODO: Items
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
                var prop = GetOrCreateProperty(env.Key, env.Value);
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

        private Property GetOrCreateProperty(string name, string value)
        {
            var prop = _db.Properties.FirstOrDefault(p => p.Name == name && p.Value == value);
            if (prop == null)
            {
                prop = new Property
                {
                    Name = name,
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
