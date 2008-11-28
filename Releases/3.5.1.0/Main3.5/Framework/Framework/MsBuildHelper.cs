//-----------------------------------------------------------------------
// <copyright file="MsBuildHelper.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Framework
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Escape</i> (<b>Required: </b> InputString <b>Output: </b> OutputString)</para>
    /// <para><i>GetCommonItems</i> (<b>Required: </b> InputItems1, InputItems2 <b>Output: </b> OutputItems, ItemCount)</para>
    /// <para><i>GetCurrentDirectory</i> (<b>Output: </b> CurrentDirectory)</para>
    /// <para><i>GetDistinctItems</i> (<b>Required: </b> InputItems1, InputItems2 <b>Output: </b> OutputItems, ItemCount)</para>
    /// <para><i>GetItem</i> (<b>Required: </b> InputItems1, Position<b>Output: </b> OutputItems)</para>
    /// <para><i>GetItemCount</i> (<b>Required: </b> InputItems1 <b>Output: </b> ItemCount)</para>
    /// <para><i>GetLastItem</i> (<b>Required: </b> InputItems1<b>Output: </b> OutputItems)</para>
    /// <para><i>RemoveDuplicateFiles</i> (<b>Required: </b> InputItems1 <b>Output: </b> OutputItems, ItemCount)</para>
    /// <para><i>Sort</i> (<b>Required: </b> InputItems1<b>Output: </b> OutputItems)</para>
    /// <para><i>StringToItemCol</i> (<b>Required: </b> ItemString, Separator <b>Output: </b> OutputItems, ItemCount)</para>
    /// <para><b>Remote Execution Support:</b> NA</para>
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Project ToolsVersion="3.5" DefaultTargets="Default" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    ///     <PropertyGroup>
    ///         <TPath>$(MSBuildProjectDirectory)\..\MSBuild.ExtensionPack.tasks</TPath>
    ///         <TPath Condition="Exists('$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks')">$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks</TPath>
    ///     </PropertyGroup>
    ///     <Import Project="$(TPath)"/>
    ///     <Target Name="Default">
    ///         <ItemGroup>
    ///             <!-- Define some collections to use in the samples-->
    ///             <Col1 Include="hello"/>
    ///             <Col1 Include="how"/>
    ///             <Col1 Include="are"/>
    ///             <Col2 Include="you"/>
    ///             <Col3 Include="hello"/>
    ///             <Col3 Include="bye"/>
    ///             <DuplicateFiles Include="C:\Demo\**\*"/>
    ///         </ItemGroup>
    ///         <!-- Escape a string with special MSBuild characters -->
    ///         <MSBuild.ExtensionPack.Framework.MsBuildHelper TaskAction="Escape" InString="hello how;are *you">
    ///             <Output TaskParameter="OutString" PropertyName="out"/>
    ///         </MSBuild.ExtensionPack.Framework.MsBuildHelper>
    ///         <Message Text="OutString: $(out)"/>
    ///         <!-- Sort an ItemGroup alphabetically -->
    ///         <MSBuild.ExtensionPack.Framework.MsBuildHelper TaskAction="Sort" InputItems1="@(Col1)">
    ///             <Output TaskParameter="OutputItems" ItemName="sorted"/>
    ///         </MSBuild.ExtensionPack.Framework.MsBuildHelper>
    ///         <Message Text="Sorted Items: %(sorted.Identity)"/>
    ///         <!-- Get a single item by position -->
    ///         <MSBuild.ExtensionPack.Framework.MsBuildHelper TaskAction="GetItem" InputItems1="@(Col1)" Position="2">
    ///             <Output TaskParameter="OutputItems" ItemName="AnItem"/>
    ///         </MSBuild.ExtensionPack.Framework.MsBuildHelper>
    ///         <Message Text="Item: %(AnItem.Identity)"/>
    ///         <!-- Get the last item -->
    ///         <MSBuild.ExtensionPack.Framework.MsBuildHelper TaskAction="GetLastItem" InputItems1="@(Col1)">
    ///             <Output TaskParameter="OutputItems" ItemName="LastItem"/>
    ///         </MSBuild.ExtensionPack.Framework.MsBuildHelper>
    ///         <Message Text="Last Item: %(LastItem.Identity)"/>
    ///         <!-- Get common items. Note that this can be accomplished without using a custom task. -->
    ///         <MSBuild.ExtensionPack.Framework.MsBuildHelper TaskAction="GetCommonItems" InputItems1="@(Col1)" InputItems2="@(Col3)">
    ///             <Output TaskParameter="OutputItems" ItemName="comm"/>
    ///         </MSBuild.ExtensionPack.Framework.MsBuildHelper>
    ///         <Message Text="Common Items: %(comm.Identity)"/>
    ///         <!-- Get distinct items. Note that this can be accomplished without using a custom task. -->
    ///         <MSBuild.ExtensionPack.Framework.MsBuildHelper TaskAction="GetDistinctItems" InputItems1="@(Col1)" InputItems2="@(Col3)">
    ///             <Output TaskParameter="OutputItems" ItemName="distinct"/>
    ///         </MSBuild.ExtensionPack.Framework.MsBuildHelper>
    ///         <Message Text="Distinct Items: %(distinct.Identity)"/>
    ///         <!-- Remove duplicate files. This can accomplish a large performance gain in some copy operations -->
    ///         <MSBuild.ExtensionPack.Framework.MsBuildHelper TaskAction="RemoveDuplicateFiles" InputItems1="@(DuplicateFiles)">
    ///             <Output TaskParameter="OutputItems" ItemName="NewCol1"/>
    ///         </MSBuild.ExtensionPack.Framework.MsBuildHelper>
    ///         <Message Text="Full File List contains: %(DuplicateFiles.Identity)"/>
    ///         <Message Text="Removed Duplicates Contains: %(NewCol1.Identity)"/>
    ///         <!-- Get the number of items in a collection -->
    ///         <MSBuild.ExtensionPack.Framework.MsBuildHelper TaskAction="GetItemCount" InputItems1="@(NewCol1)">
    ///             <Output TaskParameter="ItemCount" PropertyName="MyCount"/>
    ///         </MSBuild.ExtensionPack.Framework.MsBuildHelper>
    ///         <Message Text="$(MyCount)"/>
    ///         <!-- Convert a seperated list to an ItemGroup -->
    ///         <MSBuild.ExtensionPack.Framework.MsBuildHelper TaskAction="StringToItemCol" ItemString="how,how,are,you" Separator=",">
    ///             <Output TaskParameter="OutputItems" ItemName="NewCol11"/>
    ///         </MSBuild.ExtensionPack.Framework.MsBuildHelper>
    ///         <Message Text="String Item Collection contains: %(NewCol11.Identity)"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    public class MSBuildHelper : BaseTask
    {
        private const string EscapeTaskAction = "Escape";
        private const string GetCommonItemsTaskAction = "GetCommonItems";
        private const string GetCurrentDirectoryTaskAction = "GetCurrentDirectory";
        private const string GetDistinctItemsTaskAction = "GetDistinctItems";
        private const string GetItemTaskAction = "GetItem";
        private const string GetItemCountTaskAction = "GetItemCount";
        private const string GetLastItemTaskAction = "GetLastItem";
        private const string RemoveDuplicateFilesTaskAction = "RemoveDuplicateFiles";
        private const string SortTaskAction = "Sort";
        private const string StringToItemColTaskAction = "StringToItemCol";
        
        private List<ITaskItem> inputItems1;
        private List<ITaskItem> inputItems2;
        private List<ITaskItem> outputItems;

        [DropdownValue(EscapeTaskAction)]
        [DropdownValue(GetCommonItemsTaskAction)]
        [DropdownValue(GetCurrentDirectoryTaskAction)]
        [DropdownValue(GetDistinctItemsTaskAction)]
        [DropdownValue(GetItemTaskAction)]
        [DropdownValue(GetItemCountTaskAction)]
        [DropdownValue(GetLastItemTaskAction)]
        [DropdownValue(RemoveDuplicateFilesTaskAction)]
        [DropdownValue(SortTaskAction)]
        [DropdownValue(StringToItemColTaskAction)]
        public override string TaskAction
        {
            get { return base.TaskAction; }
            set { base.TaskAction = value; }
        }
        
        /// <summary>
        /// Gets the current directory
        /// </summary>
        [Output]
        [TaskAction(GetCurrentDirectoryTaskAction, false)]
        public string CurrentDirectory { get; set; }

        /// <summary>
        /// Sets the position of the Item to get
        /// </summary>
        [TaskAction(GetItemTaskAction, true)]
        public int Position { get; set; }

        /// <summary>
        /// Sets the string to convert to a Task Item
        /// </summary>
        [TaskAction(StringToItemColTaskAction, true)]
        public string ItemString { get; set; }

        /// <summary>
        /// Sets the separator to use to split the ItemString when calling StringToItemCol
        /// </summary>
        [TaskAction(StringToItemColTaskAction, true)]
        public string Separator { get; set; }

        /// <summary>
        /// Sets the input string
        /// </summary>
        public string InString { get; set; }
        
        /// <summary>
        /// Gets the output string
        /// </summary>
        [Output]
        public string OutString { get; set; }

        /// <summary>
        /// Sets InputItems1.
        /// </summary>
        [TaskAction(GetCommonItemsTaskAction, true)]
        [TaskAction(GetDistinctItemsTaskAction, true)]
        [TaskAction(GetItemTaskAction, true)]
        [TaskAction(GetItemCountTaskAction, true)]
        [TaskAction(GetLastItemTaskAction, true)]
        [TaskAction(RemoveDuplicateFilesTaskAction, true)]
        [TaskAction(SortTaskAction, true)]
        public ITaskItem[] InputItems1
        {
            get { return this.inputItems1.ToArray(); }
            set { this.inputItems1 = new List<ITaskItem>(value); }
        }

        /// <summary>
        /// Sets InputItems2.
        /// </summary>
        [TaskAction(GetCommonItemsTaskAction, true)]
        [TaskAction(GetDistinctItemsTaskAction, true)]
        public ITaskItem[] InputItems2
        {
            get { return this.inputItems2.ToArray(); }
            set { this.inputItems2 = new List<ITaskItem>(value); }
        }

        /// <summary>
        /// Gets the OutputItems.
        /// </summary>
        [Output]
        [TaskAction(GetCommonItemsTaskAction, false)]
        [TaskAction(GetDistinctItemsTaskAction, false)]
        [TaskAction(GetItemTaskAction, false)]
        [TaskAction(GetLastItemTaskAction, false)]
        [TaskAction(RemoveDuplicateFilesTaskAction, false)]
        [TaskAction(SortTaskAction, true)]
        [TaskAction(StringToItemColTaskAction, false)]
        public ITaskItem[] OutputItems
        {
            get { return this.outputItems == null ? null : this.outputItems.ToArray(); }
            set { this.outputItems = new List<ITaskItem>(value); }
        }

        /// <summary>
        /// Gets the ItemCount.
        /// </summary>
        [Output]
        [TaskAction(GetCommonItemsTaskAction, false)]
        [TaskAction(GetDistinctItemsTaskAction, false)]
        [TaskAction(GetItemCountTaskAction, false)]
        [TaskAction(RemoveDuplicateFilesTaskAction, false)]
        [TaskAction(StringToItemColTaskAction, false)]
        public int ItemCount { get; set; }

        protected override void InternalExecute()
        {
            if (!this.TargetingLocalMachine())
            {
                return;
            }

            switch (this.TaskAction)
            {
                case "Escape":
                    this.Escape();
                    break;
                case "RemoveDuplicateFiles":
                    this.RemoveDuplicateFiles();
                    break;
                case "GetItemCount":
                    this.GetItemCount();
                    break;
                case "GetItem":
                    this.GetItem();
                    break;
                case "GetLastItem":
                    this.GetLastItem();
                    break;
                case "GetCommonItems":
                    this.GetCommonItems();
                    break;
                case "GetDistinctItems":
                    this.GetDistinctItems();
                    break;
                case "GetCurrentDirectory":
                    this.GetCurrentDirectory();
                    break;
                case "Sort":
                    this.Sort();
                    break;
                case "StringToItemCol":
                    this.StringToItemCol();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        private void Escape()
        {
            if (string.IsNullOrEmpty(this.InString))
            {
                Log.LogError("InString is required");
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Escaping string: {0}", this.InString));
            this.OutString = Microsoft.Build.BuildEngine.Utilities.Escape(this.InString);
        }

        private void Sort()
        {
            this.LogTaskMessage("Sorting Items");
            if (this.inputItems1 == null)
            {
                Log.LogError("InputItems1 is required");
                return;
            }

            this.outputItems = new List<ITaskItem>();
            ArrayList sortedItems = new ArrayList(this.InputItems1.Length);
            
            foreach (ITaskItem item in this.InputItems1)
            {
                sortedItems.Add(item.ItemSpec);
            }

            sortedItems.Sort();
            foreach (string s in sortedItems)
            {
                foreach (ITaskItem item in this.InputItems1)
                {
                    if (item.ItemSpec == s)
                    {
                        this.outputItems.Add(item);
                        break;
                    }
                }
            }
        }

        private void GetItem()
        {
            this.LogTaskMessage("Getting Item");
            if (this.inputItems1 == null)
            {
                Log.LogError("InputItems1 is required");
                return;
            }

            if (this.Position > this.InputItems1.Length - 1)
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Position: {0} is outside the size of the item collection: {1}", this.Position, this.InputItems1.Length));
            }

            this.outputItems = new List<ITaskItem> { this.inputItems1[this.Position] };
        }

        private void GetLastItem()
        {
            this.LogTaskMessage("Getting Last Item");
            if (this.inputItems1 == null)
            {
                Log.LogError("InputItems1 is required");
                return;
            }

            this.outputItems = new List<ITaskItem> { this.inputItems1[this.inputItems1.Count - 1] };
        }

        private void GetDistinctItems()
        {
            this.LogTaskMessage("Getting Distinct Items");
            this.outputItems = new List<ITaskItem>();
            if (this.inputItems1 == null)
            {
                Log.LogError("InputItems1 is required");
                return;
            }

            if (this.inputItems2 == null)
            {
                Log.LogError("InputItems2 is required");
                return;
            }

            foreach (ITaskItem item in this.InputItems1)
            {
                bool found = false;

                // we only match on itemspec.
                foreach (ITaskItem item2 in this.inputItems2)
                {
                    if (item.ItemSpec == item2.ItemSpec)
                    {
                        found = true;
                    }
                }

                if (!found)
                {
                    this.outputItems.Add(item);
                }
            }

            foreach (ITaskItem item in this.InputItems2)
            {
                bool found = false;

                // we only match on itemspec.
                foreach (ITaskItem item2 in this.InputItems1)
                {
                    if (item.ItemSpec == item2.ItemSpec)
                    {
                        found = true;
                    }
                }

                if (!found)
                {
                    this.outputItems.Add(item);
                }
            }

            this.ItemCount = this.outputItems.Count;
        }

        private void GetCommonItems()
        {
            this.LogTaskMessage("Getting Common Items");
            this.outputItems = new List<ITaskItem>();
            if (this.inputItems1 == null)
            {
                Log.LogError("InputItems1 is required");
                return;
            }

            if (this.inputItems2 == null)
            {
                Log.LogError("InputItems2 is required");
                return;
            }

            foreach (ITaskItem item in this.inputItems1)
            {
                bool found = false;

                // we only match on itemspec.
                foreach (ITaskItem item2 in this.inputItems2)
                {
                    if (item.ItemSpec == item2.ItemSpec)
                    {
                        found = true;
                    }
                }

                if (found)
                {
                    this.outputItems.Add(item);
                }
            }

            this.ItemCount = this.outputItems.Count;
        }

        private void StringToItemCol()
        {
            this.LogTaskMessage("Converting String To Item Collection");

            if (string.IsNullOrEmpty(this.ItemString))
            {
                Log.LogError("ItemString is required");
                return;
            }

            if (string.IsNullOrEmpty(this.Separator))
            {
                Log.LogError("Separator is required");
                return;
            }

            this.outputItems = new List<ITaskItem>();
            string[] s = this.ItemString.Split(new[] { this.Separator }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string newItem in s)
            {
                this.outputItems.Add(new TaskItem(newItem));
            }

            this.ItemCount = this.outputItems.Count;
        }

        private void RemoveDuplicateFiles()
        {
            this.LogTaskMessage("Removing Duplicates");
            if (this.inputItems1 == null)
            {
                Log.LogError("InputItems1 is required");
                return;
            }

            this.outputItems = new List<ITaskItem>();
            ArrayList names = new ArrayList();

            foreach (ITaskItem item in this.InputItems1)
            {
                FileInfo f = new FileInfo(item.ItemSpec);
                if (!names.Contains(f.Name))
                {
                    names.Add(f.Name);
                    this.outputItems.Add(item);
                }
            }

            this.ItemCount = this.outputItems.Count;
        }

        private void GetCurrentDirectory()
        {
            this.LogTaskMessage("Getting Current Directory");
            System.IO.FileInfo projFile = new System.IO.FileInfo(BuildEngine.ProjectFileOfTaskNode);

            if (projFile.Directory != null)
            {
                this.CurrentDirectory = projFile.Directory.FullName;
            }
        }

        private void GetItemCount()
        {
            this.LogTaskMessage("Getting Item Count");
            this.ItemCount = this.InputItems1.Length;
        }
    }
}