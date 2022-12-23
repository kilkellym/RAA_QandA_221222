#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Diagnostics;

#endregion

namespace RAA_QandA_221222
{
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        public Result Execute(
          ExternalCommandData commandData,
          ref string message,
          ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            ICollection<ElementId> colls = ExternalFileUtils.GetAllExternalFileReferences(doc);
            if (colls.Count > 0)
            {
                foreach (var id in colls)
                {
                    Element ele = doc.GetElement(id);

                    if (ele != null && ele is RevitLinkType)
                    {
                        RevitLinkType rvtLinkType = (RevitLinkType)ele;

                        Document linkdoc = rvtLinkType.Document;

                        // ----------------------------------
                        List<WorksetId> lstWkSet_Close = new List<WorksetId>();
                        List<WorksetId> lstWkSet_Open = new List<WorksetId>();

                        List<Workset> wsLinkList = GetAllUserWorksets(doc);

                        foreach(Workset curWS in wsLinkList)
                        {
                            if (curWS.Name == "Shared Levels and Grids")
                           
                                lstWkSet_Close.Add(curWS.Id);
                            else
                                lstWkSet_Open.Add(curWS.Id);
                        }

                        using(Transaction t = new Transaction(doc))
                        {
                            t.Start("hide workset");
                            foreach (WorksetId curWS in lstWkSet_Close)
                            {
                                HideWorkset(doc, doc.ActiveView, curWS);
                            }
                            t.Commit();
                        }
                    }
                }
            }

            return Result.Succeeded;
        }

        public static List<string> GetUsedFilterNames(Document curDoc)
        {
            FilteredElementCollector usedFilters = new FilteredElementCollector(curDoc).OfClass(typeof(View));

            IList<Element> elements = usedFilters.ToElements();
            List<ElementId> usedFilterIds = new List<ElementId>();
            List<string> usedFilterNames = new List<string>();

            foreach (View v in elements)
            {
                try
                {
                    foreach (ElementId eId in v.GetFilters())
                    {
                        //add filter Ids to the ussedFilterIds list
                        usedFilterIds.Add(eId);
                    }
                }
                catch (Autodesk.Revit.Exceptions.InvalidOperationException)
                {
                    continue;
                }
            }
            //Get filter name
            foreach (ElementId ef in usedFilterIds)
            {
                Element element = curDoc.GetElement(ef);

                string fNam = element.Name;
                usedFilterNames.Add(fNam);
            }

            return usedFilterNames;
        }

        public static List<Workset> GetAllUserWorksets(Document curDoc)
        {
            FilteredWorksetCollector wsCol = new FilteredWorksetCollector(curDoc);
            wsCol.OfKind(WorksetKind.UserWorkset);

            List<Workset> wsList = new List<Workset>();

            foreach (Workset ws in wsCol)
            {
                wsList.Add(ws);
            }

            return wsList;
        }

        public void HideWorkset(Document doc, View view, WorksetId worksetId)
        {
            // get the current visibility
            WorksetVisibility visibility = view.GetWorksetVisibility(worksetId);

            // and set it to 'Hidden' if it is not hidden yet
            if (visibility != WorksetVisibility.Hidden)
            {
                view.SetWorksetVisibility(worksetId, WorksetVisibility.Hidden);
            }

            // Get the workset’s default visibility      
            WorksetDefaultVisibilitySettings defaultVisibility = WorksetDefaultVisibilitySettings.GetWorksetDefaultVisibilitySettings(doc);

            // and making sure it is set to 'false'
            if (defaultVisibility.IsWorksetVisible(worksetId))
            {
                defaultVisibility.SetWorksetVisibility(worksetId, false);
            }
        }
    }
}