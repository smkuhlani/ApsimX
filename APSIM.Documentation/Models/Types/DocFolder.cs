using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;
using System.Linq;
using Models;
using System.Collections.Immutable;
using Models.Factorial;
using System.Data;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Base documentation class for models
    /// </summary>
    public class DocFolder : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocGenericWithChildren" /> class.
        /// </summary>
        public DocFolder(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override IEnumerable<ITag> Document(List<ITag> tags = null, int headingLevel = 0, int indent = 0)
        {
            if (tags == null)
                tags = new List<ITag>();

            var subTags = new List<ITag>();

            // Write memos.
            var memoTags = new List<ITag>();
            foreach (Memo memo in model.FindAllChildren<Memo>().Where(memo => memo.Enabled))
            {
                memoTags.AddRange(AutoDocumentation.Document(memo));
            }
            subTags.Add(new Section(memoTags));

            foreach(Map map in model.FindAllChildren<Map>().Where(map => map.Enabled))
            {
                subTags.AddRange(AutoDocumentation.Document(map));
            }

            // Write experiment descriptions. We don't call experiment.Document() here,
            // because we want to just show the experiment design (a string) and put it
            // inside a table cell.
            IEnumerable<Experiment> experiments = model.FindAllChildren<Experiment>().Where(experiment => experiment.Enabled);
            if (experiments.Any())
            {
                var experimentsTag = new List<ITag>();
                DataTable table = new DataTable();
                table.Columns.Add("Experiment Name", typeof(string));
                table.Columns.Add("Design (Number of Treatments)", typeof(string));

                foreach (Experiment experiment in experiments)
                {
                    DataRow row = table.NewRow();
                    row[0] = experiment.Name;
                    row[1] = experiment.GetDesign();
                    table.Rows.Add(row);
                }
                experimentsTag.Add(new Paragraph("**List of experiments.**"));
                experimentsTag.Add(new Table(table));
                subTags.Add(new Section("Experiments", experimentsTag));

            }
            else
            {
                // No experiments - look for free standing simulations.
                foreach (Simulation simulation in model.FindAllChildren<Simulation>().Where(simulation => simulation.Enabled))
                {
                    var graphPageTags = new List<ITag>();
                    foreach (Folder folder in simulation.FindAllChildren<Folder>().Where(folder => folder.Enabled && folder.ShowInDocs))
                    {
                        var childGraphs = (model as Folder).GetChildGraphs(folder);
                        while (childGraphs.Any())
                        {
                            foreach(Shared.Documentation.Graph graph in childGraphs)
                                graphPageTags.Add(graph);
                        }
                    }
                    subTags.Add(new Paragraph($"**{simulation.Name}**"));
                    subTags.Add(new Section(graphPageTags));
                }
            }

            // Write page of graphs.
            if ((model as Folder).ShowInDocs)
            {
                if (model.Parent != null)
                {
                    var childGraphs = new List<Shared.Documentation.Graph>();
                    if ((model as Folder).GetChildGraphs(model.Parent) != null)
                    {
                        while (childGraphs.Any())
                            subTags.Add(new Shared.Documentation.GraphPage(childGraphs));
                    }
                }
            }

            // Document experiments individually.
            foreach (Experiment experiment in experiments.Where(expt => expt.Enabled))
                subTags.AddRange(AutoDocumentation.Document(experiment));

            // Document child folders.
            foreach (Folder folder in model.FindAllChildren<Folder>().Where(f => f.Enabled))
                subTags.AddRange(AutoDocumentation.Document(folder));

            tags.Add(new Section(model.Name, subTags));
            return tags;
        }

    }
}
