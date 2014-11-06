﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using System.Xml.Linq;
using BExIS.Web.Shell.Areas.DIM.Models;
using BExIS.Dlm.Entities.Data;
using BExIS.Dlm.Services.Data;
using BExIS.Xml.Services;
using BExIS.Xml.Services.Mapping;
using Vaiona.Util.Cfg;
using BExIS.Dlm.Services.MetadataStructure;
using BExIS.Dlm.Entities.MetadataStructure;

namespace BExIS.Web.Shell.Areas.DIM.Controllers
{
    public class AdminController : Controller
    {

        private List<long> datasetVersionIds = new List<long>();
        private XmlMapperManager xmlMapperManager = new XmlMapperManager();
        
        //
        // GET: /DIM/Admin/

        public ActionResult Index()
        {
            AdminModel model = new AdminModel();

            MetadataStructureManager metadataStructureManager = new MetadataStructureManager();
            IList<MetadataStructure> metadataStructures = metadataStructureManager.Repo.Get();

            foreach(MetadataStructure metadataStructure in metadataStructures)
            {
                model.Add(metadataStructure);
            }
            
            return View(model);
        }



        public ActionResult LoadMetadataStructureTab(long Id)
        {
            #region load Model

                DatasetManager datasetManager = new DatasetManager();
                datasetVersionIds = datasetManager.GetDatasetLatestIds();

                MetadataStructureManager metadataStructureManager = new MetadataStructureManager();
                MetadataStructure metadataStructure = metadataStructureManager.Repo.Get(Id);

                MetadataStructureModel model = new MetadataStructureModel(
                        metadataStructure.Id,
                        metadataStructure.Name,
                        metadataStructure.Description,
                        getDatasetVersionsDic(metadataStructure,datasetVersionIds)
                
                    );

            #endregion

            return PartialView("_metadataStructureView",model);
        }


        public ActionResult ConvertSelectedDatasetVersion(string Id, string SelectedDatasetIds)
        {

            #region load Model

                DatasetManager datasetManager = new DatasetManager();
                datasetVersionIds = datasetManager.GetDatasetLatestIds();

                MetadataStructureManager metadataStructureManager = new MetadataStructureManager();
                MetadataStructure metadataStructure = metadataStructureManager.Repo.Get(Convert.ToInt64(Id));

                MetadataStructureModel model = new MetadataStructureModel(
                        metadataStructure.Id,
                        metadataStructure.Name,
                        metadataStructure.Description,
                        getDatasetVersionsDic(metadataStructure,datasetVersionIds)
                
                    );

            #endregion

            #region convert

            if (SelectedDatasetIds != null && SelectedDatasetIds!="")
            {

                string[] ids = SelectedDatasetIds.Split(',');

                foreach (string id in ids)
                {
                    string path = Export(Convert.ToInt64(id));
                    model.AddMetadataPath(Convert.ToInt64(id), path);
                }
            }

            #endregion


            return PartialView("_metadataStructureView",model);
        }

        public ActionResult Download(string path)
        {
            return File(path, "text/xml");
        }

        //public ActionResult ConvertMetadataToABCD()
        //{
        //    string path_mapping_abcd = Path.Combine(AppConfiguration.GetModuleWorkspacePath("DIM"), "mapping_abcd.xml");

        //    XmlMapperManager xmlMapperManager = new XmlMapperManager();

        //    xmlMapperManager.Load(path_mapping_abcd);

        //    DatasetManager datasetManager = new DatasetManager();

        //    List<long> ids = datasetManager.GetDatasetLatestIds();


        //    foreach (long id in ids)
        //    {
        //        Dataset dataset = datasetManager.GetDataset(id);
        //        if (dataset.MetadataStructure.Id.Equals(xmlMapperManager.xmlMapper.Id))
        //        {
        //            XmlDocument metadata = datasetManager.GetDatasetLatestMetadataVersion(id);
        //            xmlMapperManager.Export(metadata, id);
        //        }
        //    }

        //    return View("Index");
        //}

        private string Export(long datasetVersionId)
        {
            DatasetManager datasetManager = new DatasetManager();
            DatasetVersion datasetVersion = datasetManager.GetDatasetVersion(datasetVersionId);

            string fileName = getMappingFileName(datasetVersion);
            string path_mapping_file = "";
            try
            {
                    path_mapping_file = Path.Combine(AppConfiguration.GetModuleWorkspacePath("DIM"), fileName);

                    xmlMapperManager = new XmlMapperManager();
                    xmlMapperManager.Load(path_mapping_file);

                    return xmlMapperManager.Export(datasetVersion.Metadata, datasetVersion.Id);
            }
            catch
            {
            
            }

            return "";
        }


        //public ActionResult ConvertMetadataToEML()
        //{
        //    string path_mapping_eml = Path.Combine(AppConfiguration.GetModuleWorkspacePath("DIM"), "mapping_eml.xml");

        //    XmlMapperManager xmlMapperManager = new XmlMapperManager();
        //    //xmlMapperManager.SearchForSequenceByDuplicatedElementNames = false;

        //    xmlMapperManager.Load(path_mapping_eml);

        //    DatasetManager datasetManager = new DatasetManager();

        //    List<long> ids = datasetManager.GetDatasetLatestIds();

        //    foreach (long id in ids)
        //    {
        //        Dataset dataset = datasetManager.GetDataset(id);
        //        if (dataset.MetadataStructure.Id.Equals(xmlMapperManager.xmlMapper.Id))
        //        {
        //            XmlDocument metadata = datasetManager.GetDatasetLatestMetadataVersion(id);
        //            xmlMapperManager.Export(metadata, id);
        //        }
        //    }

        //    return View("Index");
        //}


        #region helper

        private string getTitle(DatasetVersion datasetVersion)
        {
         
            // get MetadataStructure 
            XDocument xDoc = XmlUtility.ToXDocument((XmlDocument)datasetVersion.Dataset.MetadataStructure.Extra);
            XElement temp = XmlUtility.GetXElementByAttribute("nodeRef", "name", "title", xDoc);

            string xpath = temp.Attribute("value").Value.ToString();
            string title = datasetVersion.Metadata.SelectSingleNode(xpath).InnerText;

            return title;
        }

        private string getMappingFileName(DatasetVersion datasetVersion)
        {
            // get MetadataStructure 
            XDocument xDoc = XmlUtility.ToXDocument((XmlDocument)datasetVersion.Dataset.MetadataStructure.Extra);
            XElement temp = XmlUtility.GetXElementByAttribute("convertRef", "name", "mappingFile", xDoc);

            return temp.Attribute("value").Value.ToString();
        }

        private List<DatasetVersionModel> getDatasetVersionsDic(MetadataStructure metadataStructure, List<long> datasetVersionIds)
        {
            List<DatasetVersionModel> datasetVersions = new List<DatasetVersionModel>();
            DatasetManager datasetManager = new DatasetManager();

            DatasetVersion datasetVersion;

            foreach(long id in datasetVersionIds)
            {
                datasetVersion = datasetManager.GetDatasetVersion(id);
                if(datasetVersion.Dataset.MetadataStructure.Id.Equals(metadataStructure.Id))
                {
                    datasetVersions.Add(
                        new DatasetVersionModel
                        {
                            Id = id,
                            Title = getTitle(datasetVersion),
                            MetadataDownloadPath = ""
                        });

                }

            }

            return datasetVersions;
        }

        #endregion


    }
}