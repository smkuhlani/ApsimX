using System;
using System.Collections.Generic;
using Models.Core;

namespace Models
{

    /// <summary>
    /// A report class for writing output to the data store.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PlaylistView")]
    [PresenterName("UserInterface.Presenters.PlaylistPresenter")]
    [ValidParent(ParentType = typeof(Simulations))]
    public class Playlist : Model
    {
        //// <summary>Link to simulations</summary>
        //[Link]
       // private Simulations simulations = null;

        /// <summary>Gets or sets the memo text.</summary>
        [Description("Text of the playlist")]
        public string Text { get; set; }

        /// <summary>
        /// Returns the name of all simulations that match the text
        /// </summary>
        public List<string> GetListOfSimulations()
        {
            List<string> names = new List<string>();
            names.Add(Text);
            return names;
        }
    }
}