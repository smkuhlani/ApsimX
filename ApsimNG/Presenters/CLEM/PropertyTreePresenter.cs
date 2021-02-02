﻿
namespace UserInterface.Presenters
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Xml;
    using APSIM.Shared.Utilities;
    using Commands;
    using EventArguments;
    using Interfaces;
    using Models;
    using Models.Core;
    using Views;
    
    /// <summary>
    /// Class used to create the heirachy of the the categories and subcategories from the 
    /// [Category] attribute added to the properties of a model.
    /// </summary>
    public class CategoryItem
    {
        public string Name;

        /// <summary>
        /// Subcategories of this category
        /// </summary>
        public List<string> SubcategoryNames;

        /// <summary>
        /// Constructor 
        /// </summary>
        public  CategoryItem(string name)
        {
            this.Name = name;
            this.SubcategoryNames = new List<string>();
        }

        public void AddSubcategoryName(string name)
        {
            //is subcategory name already in the list
            bool subcatExists = this.SubcategoryNames.Exists(subcatname => subcatname == name);
            // if it isn't then add it.
            if (!subcatExists)
            {
                this.SubcategoryNames.Add(name);
            }
        }
    }

    public class CategoryTree
    {
        public List<CategoryItem> CategoryItems;

        public CategoryTree()
        {
            this.CategoryItems = new List<CategoryItem>();
        }

        public CategoryItem FindCategoryInTree(string catName)
        {
            return this.CategoryItems.Find(item => item.Name == catName);
        }

        public void AddCategoryToTree(String catName)
        {
            bool catExists = this.CategoryItems.Exists(item => item.Name == catName);
            if (!catExists)
            {
                this.CategoryItems.Add(new CategoryItem(catName));
            }
        }
    }

    /// <summary>
    /// This presenter class is responsible for populating the view
    /// passed into the constructor and handling all user interaction of
    /// the view. Humble Dialog pattern.
    /// </summary>
    public class PropertyTreePresenter : IPresenter
    {
        /// <summary>The visual instance</summary>
        private IPropertyTreeView treeview;
        private IPropertyView propertyView;

        /// <summary>Presenter for the component</summary>
        private SimplePropertyPresenter propertyPresenter;

        /// <summary>
        /// The model we're going to examine for properties.
        /// </summary>
        private Model model;

        /// <summary>
        /// The category name to filter for on the Category Attribute for the properties
        /// </summary>
        private string SelectedCategory { get; set; }

        /// <summary>
        /// The subcategory name to filter for on the Category Attribute for the properties
        /// </summary>
        private string SelectedSubCategory { get; set; }

        /// <summary>Initializes a new instance of the <see cref="PropertyTreePresenter" /> class</summary>
        public PropertyTreePresenter()
        {
        }

        /// <summary>Gets or sets the width of the explorer tree panel</summary>
        /// <value>The width of the tree.</value>
        public int TreeWidth
        {
            get { return this.treeview.TreeWidth; }
            set { this.treeview.TreeWidth = value; }
        }

        /// <summary>
        /// The parent ExplorerPresenter.
        /// </summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>Gets the current right hand presenter.</summary>
        /// <value>The current presenter.</value>
        public SimplePropertyPresenter PropertyPresenter
        {
            get
            {
                return this.propertyPresenter;
            }
        }

        /// <summary>Gets the path of the current selected node in the tree.</summary>
        /// <value>The current node path.</value>
        public string CurrentNodePath
        {
            get
            {
                return this.treeview.SelectedNode;
            }
        }

        /// <summary>
        /// Attach the view to this presenter and begin populating the view.
        /// </summary>
        /// <param name="model">The simulation model</param>
        /// <param name="view">The view used for display</param>
        /// <param name="explorerPresenter">The presenter for this object</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.model = model as Model;
            this.treeview = view as IPropertyTreeView;
            this.TreeWidth = 180; 
            this.treeview.SelectedNodeChanged += this.OnNodeSelected;
            this.explorerPresenter = explorerPresenter;

            //Fill in the nodes in the tree view
            this.RefreshTreeView();

            //Initialise the Right Hand View
            this.propertyPresenter = new SimplePropertyPresenter();
            this.ShowRightHandView();
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            this.treeview.SelectedNodeChanged -= this.OnNodeSelected;

            this.HideRightHandView();
            if (this.treeview is Views.PropertyTreeView)
            {
                (this.treeview as Views.PropertyTreeView).MainWidget.Destroy();
            }
        }

        /// <summary>
        /// Refresh the treeview.
        /// </summary>
        public void RefreshTreeView()
        {
            CategoryTree categoryTree = this.GetPropertyCategories();
            this.treeview.Refresh(this.GetNodeDescription(categoryTree));
        }

        /// <summary>Select a node in the view.</summary>
        /// <param name="nodePath">Path to node</param>
        public void SelectNode(string nodePath)
        {
            this.treeview.SelectedNode = nodePath;
            this.HideRightHandView();
            this.ShowRightHandView();
        }

        /// <summary>Hide the right hand panel.</summary>
        public void HideRightHandView()
        {
            if (this.propertyPresenter != null)
            {
                try
                {
                    this.propertyPresenter.Detach();
                }
                catch (Exception err)
                {
                    throw new Exception(err.Message);
                }
            }

            this.SelectedCategory = "";
            this.SelectedSubCategory = "";
            this.treeview.AddRightHandView(null); //add an Empty right hand view
        }

        /// <summary>Display a view on the right hand panel in view.</summary>
        public void ShowRightHandView()
        {
            if (this.treeview.SelectedNode != string.Empty)
            {
                string[] path = this.treeview.SelectedNode.Split('.');
                string root = "";
                string category = "";
                string subcategory = "";

                //zero based but path[0] is always empty. 
                //(because SelectedNode path always starts with a ".")
                //true root ie. name of model is always path[1]
                switch (path.Length)
                {
                    case 1:
                        root = "";
                        category = "";
                        subcategory = "";
                        break;
                    case 2:
                        root = path[1];
                        category = "";
                        subcategory = "";
                        break;
                    case 3:
                        root = path[1];
                        category = path[2] ;
                        subcategory = "";
                        break;
                    case 4:
                        root = path[1];
                        category = path[2];
                        subcategory = path[3];
                        break;
                }
                this.SelectedCategory = category;
                this.SelectedSubCategory = subcategory;
                this.propertyPresenter.Filter = IsPropertySelected;
            }
            else
            {
                //this will show all the properties in the model 
                //there will be no filtering on Category and Subcategory.
                this.propertyPresenter.Filter = null;
                this.SelectedCategory = "";
                this.SelectedSubCategory = "";
            }

            //create a new grid view to be added as a RightHandView
            //nb. the grid view is owned by the tree view not by this presenter.
            this.propertyView = new PropertyView(this.treeview as ViewBase);
            this.treeview.AddRightHandView(this.propertyView);
            this.propertyPresenter.Attach(this.model, this.propertyView, this.explorerPresenter);            
        }

        /// <summary>A node has been selected (whether by user or undo/redo)</summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Node arguments</param>
        private void OnNodeSelected(object sender, NodeSelectedArgs e)
        {
            this.HideRightHandView();
            this.ShowRightHandView();
        }

        /// <summary>
        /// Returns the Category Tree created from the Category Attributes on the properties in the model.
        /// </summary>
        /// <returns></returns>
        private CategoryTree GetPropertyCategories()
        {
            CategoryTree categories = new CategoryTree();

            if (this.model != null)
            {
                foreach (PropertyInfo property in this.model.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy))
                {
                    // Properties must have a [Description], not be called Name, and be read/write.
                    bool hasDescription = property.IsDefined(typeof(DescriptionAttribute), false);
                    bool includeProperty = hasDescription &&
                                           property.Name != "Name" &&
                                           property.CanRead &&
                                           property.CanWrite;

                    // Only allow lists that are double[], int[] or string[]
                    if (includeProperty && property.PropertyType.GetInterface("IList") != null)
                    {
                        includeProperty = property.PropertyType == typeof(double[]) ||
                                          property.PropertyType == typeof(int[]) ||
                                          property.PropertyType == typeof(string[]);
                    }

                    if (includeProperty)
                    { 
                        // Those properties with a [Catagory] attribute 
                        bool hasCategory = property.IsDefined(typeof(CategoryAttribute), false);
                        if (hasCategory)
                        {
                            //get the attribute data
                            CategoryAttribute catAtt = (CategoryAttribute)property.GetCustomAttribute(typeof(CategoryAttribute));
                            //add the category name to the list of category items
                            categories.AddCategoryToTree(catAtt.Category);
                            //add the subcategory name to the list of subcategories for the category
                            CategoryItem catItem = categories.FindCategoryInTree(catAtt.Category);
                            catItem.AddSubcategoryName(catAtt.Subcategory);
                        }
                        else
                        {
                            //If there is not [Category] attribute at all on the property in the model.
                            //Add it to the "Unspecified" Category, and "Unspecified" Subcategory
                            categories.AddCategoryToTree("Unspecified");
                            CategoryItem catItem = categories.FindCategoryInTree("Unspecified");
                            catItem.AddSubcategoryName("Unspecified");
                        }
                    }
                }
            }
            return categories;
        }
 
        /// <summary>
        /// A helper function for creating a node description object for the category hierarchy.
        /// </summary>
        /// <param name="properties">List of properties </param>
        /// <returns>The description</returns>
        private TreeViewNode GetNodeDescription(CategoryTree categoryTree)
        {
            TreeViewNode root = new TreeViewNode();
            root.Name =  model.Name;

            // find namespace and image name needed to find image file in the Resources of UserInterface project
            string nameSpace = model.GetType().FullName.Split('.')[1];
            string imageName = model.GetType().Name;
            root.ResourceNameForImage = "ApsimNG.Resources.TreeViewImages." + nameSpace + "." + imageName + ".png";

            foreach (CategoryItem cat in categoryTree.CategoryItems)
            {
                TreeViewNode description = new TreeViewNode();
                description.Name = cat.Name;
                description.ResourceNameForImage = "ApsimNG.Resources.TreeViewImages.CLEM.ActivityFolder.png";
                description.Children = new List<TreeViewNode>();
                foreach (string subcat in cat.SubcategoryNames)
                {
                    TreeViewNode child = new TreeViewNode();
                    child.Name = subcat;
                    if (subcat.ToLower().Contains("pasture"))
                    {
                        child.ResourceNameForImage = "ApsimNG.Resources.TreeViewImages.CLEM.PastureTreeItem.png";
                    }
                    else if (subcat.ToLower().Contains("unspecif"))
                    {
                        child.ResourceNameForImage = "ApsimNG.Resources.TreeViewImages.CLEM.UnspecifiedTreeItem.png";
                    }
                    else if (model.GetType().Name.ToLower().Contains("ruminant"))
                    {
                        child.ResourceNameForImage = "ApsimNG.Resources.TreeViewImages.CLEM.RuminantGroup.png";
                    }
                    else
                    {
                        child.ResourceNameForImage = "ApsimNG.Resources.TreeViewImages.CLEM.UnspecifiedTreeItem.png";
                    }
                    description.Children.Add(child);
                }
                root.Children.Add(description);
            }
            return root;
        }


        private bool IsPropertySelected(PropertyInfo property)
        {
            if ((this.SelectedCategory??"") != "") // a category has been selected
            {
                if (Attribute.IsDefined(property, typeof(CategoryAttribute), false))
                {
                    CategoryAttribute catAtt = (CategoryAttribute)Attribute.GetCustomAttribute(property, typeof(CategoryAttribute));
                    if (catAtt.Category == this.SelectedCategory)
                    {
                        if ((SelectedSubCategory ?? "") != "") // a sub category has been selected
                        {
                            // The catAtt.Subcategory is by default given a value of 
                            // "Unspecified" if the Subcategory is not assigned in the Category Attribute.
                            // so this line below will also handle "Unspecified" subcategories.
                            return (catAtt.Subcategory == this.SelectedSubCategory);
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    // if we are filtering on "Unspecified" category then there is no Category Attribute
                    // just a Description Attribute on the property in the model.
                    // So we still may need to include it in this case.
                    return (this.SelectedCategory == "Unspecified");
                }
            }
            return true;
        }
    }
}
