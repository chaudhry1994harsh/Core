﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using BExIS.Dcm.ImportMetadataStructureWizard;
using BExIS.Dcm.Wizard;
using BExIS.IO;
using BExIS.IO.Transform.Validation.Exceptions;
using BExIS.Web.Shell.Areas.DCM.Models;
using BExIS.Xml.Helpers.Mapping;
using Vaiona.Util.Cfg;

namespace BExIS.Web.Shell.Areas.DCM.Controllers
{
    public class ImportMetadataStructureSelectAFileController : Controller
    {
        //
        // GET: /DCM/ImportMetadataStructureSelectAFile/

        private ImportMetadataStructureTaskManager TaskManager;
        private FileStream Stream;

        //
        // GET: /DCM/Step1/

        [HttpGet]
        public ActionResult SelectAFile(int index)
        {
            TaskManager = (ImportMetadataStructureTaskManager)Session["TaskManager"];

            //set current stepinfo based on index
            if (TaskManager != null)
                TaskManager.SetCurrent(index);

            //Get Bus infomations
            SelectFileViewModel model = new SelectFileViewModel();
            if (TaskManager.Bus.ContainsKey(ImportMetadataStructureTaskManager.FILENAME))
            {
                model.SelectedFileName = TaskManager.Bus[ImportMetadataStructureTaskManager.FILENAME].ToString();
            }

            //get datastuctureType
            model.SupportedFileExtentions = ImportMetadataStructureWizardHelper.GetExtentionList();

            //Get StepInfo
            model.StepInfo = TaskManager.Current();

            model.serverFileList = GetServerFileList();

            return PartialView(model);
        }

        [HttpPost]
        public ActionResult SelectAFile(object[] data)
        {

            SelectFileViewModel model = new SelectFileViewModel();

            TaskManager = (ImportMetadataStructureTaskManager)Session["TaskManager"];

            if (data != null) TaskManager.AddToBus(data);

            model.StepInfo = TaskManager.Current();

            TaskManager.Current().SetValid(false);

            if (TaskManager != null)
            {
                // is path of FileStream exist
                if (TaskManager.Bus.ContainsKey(ImportMetadataStructureTaskManager.FILEPATH))
                {
                    if (IsSupportedExtention(TaskManager))
                    {
                        try
                        {
                           //check if file exist and add to taskmanager
                            string filePath = TaskManager.Bus[ImportMetadataStructureTaskManager.FILEPATH].ToString();
                            if (FileHelper.FileExist(filePath))
                            {
                                TaskManager.Current().SetValid(true);
                                TaskManager.Current().SetStatus(StepStatus.success);
                            }
                            else
                            {
                                model.ErrorList.Add(new Error(ErrorType.Other, "Cannot access FileStream on server."));
                            }

                        }
                        catch
                        {
                            model.ErrorList.Add(new Error(ErrorType.Other, "Cannot access FileStream on server."));
                        }
                    }
                    else
                    {
                        model.ErrorList.Add(new Error(ErrorType.Other, "File is not supported."));
                    }


                }
                else
                {
                    model.ErrorList.Add(new Error(ErrorType.Other, "No FileStream selected or submitted."));
                }

                try
                {
                    LoadXSDSchema(GetUserNameOrDefault());
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                    model.ErrorList.Add(new Error(ErrorType.Other, "Can not find any dependent files to the selected schema."));
                    TaskManager.Current().SetValid(false);
                }

                

                if (TaskManager.Current().IsValid())
                {
                    TaskManager.AddExecutedStep(TaskManager.Current());
                    TaskManager.GoToNext();
                    Session["TaskManager"] = TaskManager;
                    ActionInfo actionInfo = TaskManager.Current().GetActionInfo;
                    return RedirectToAction(actionInfo.ActionName, actionInfo.ControllerName, new RouteValueDictionary { { "area", actionInfo.AreaName }, { "index", TaskManager.GetCurrentStepInfoIndex() } });
                }
            }

            model.serverFileList = GetServerFileList();
            model.SupportedFileExtentions = ImportMetadataStructureWizardHelper.GetExtentionList();

            return PartialView(model);
        }

        #region private methods

        private void LoadXSDSchema(string userName)
        {
            TaskManager = (ImportMetadataStructureTaskManager)Session["TaskManager"];

            string path = TaskManager.Bus[ImportMetadataStructureTaskManager.FILEPATH].ToString();

            //open schema
            XmlSchemaManager xmlSchemaManager = new XmlSchemaManager();
            xmlSchemaManager.Load(path, userName);
             
            if (TaskManager.Bus.ContainsKey(ImportMetadataStructureTaskManager.XML_SCHEMA_MANAGER))
                TaskManager.Bus[ImportMetadataStructureTaskManager.XML_SCHEMA_MANAGER] = xmlSchemaManager;
            else
                TaskManager.Bus.Add(ImportMetadataStructureTaskManager.XML_SCHEMA_MANAGER, xmlSchemaManager);
        }

        /// <summary>
        /// Selected File store din the BUS
        /// </summary>
        /// <param name="SelectFileUploader"></param>
        /// <returns></returns>
        public ActionResult SelectFileProcess(HttpPostedFileBase SelectFileUploader)
        {
            ImportMetadataStructureTaskManager TaskManager = (ImportMetadataStructureTaskManager)Session["TaskManager"];
            if (SelectFileUploader != null)
            {
                //data/datasets/1/1/
                string dataPath = AppConfiguration.DataPath; //Path.Combine(AppConfiguration.WorkspaceRootPath, "Data");
                string storepath = Path.Combine(dataPath, "Temp", GetUserNameOrDefault());

                // if folder not exist
                if (!Directory.Exists(storepath))
                {
                    Directory.CreateDirectory(storepath);
                }

                string path = Path.Combine(storepath, SelectFileUploader.FileName);



                SelectFileUploader.SaveAs(path);
                TaskManager.AddToBus(ImportMetadataStructureTaskManager.FILEPATH, path);

                TaskManager.AddToBus(ImportMetadataStructureTaskManager.FILENAME, SelectFileUploader.FileName);
                TaskManager.AddToBus(ImportMetadataStructureTaskManager.EXTENTION, SelectFileUploader.FileName.Split('.').Last());
                Session["TaskManager"] = TaskManager;
            }

            //return RedirectToAction("UploadWizard");
            return Content("");
        }

        /// <summary>
        /// Selected File from server and store into BUS
        /// </summary>
        /// <param name="SelectFileUploader"></param>
        /// <returns></returns>
        public ActionResult SelectFileFromServerProcess(string fileName)
        {
            ImportMetadataStructureTaskManager TaskManager = (ImportMetadataStructureTaskManager)Session["TaskManager"];
            if (fileName != null)
            {
                //string path = Path.Combine(AppConfiguration.GetModuleWorkspacePath("DCM"), "ServerFiles",fileName);

                //data/datasets/1/1/
                string dataPath = AppConfiguration.DataPath; //Path.Combine(AppConfiguration.WorkspaceRootPath, "Data");
                string path = Path.Combine(dataPath, "Temp", GetUserNameOrDefault(), fileName);

                TaskManager.AddToBus(ImportMetadataStructureTaskManager.FILEPATH, path);

                TaskManager.AddToBus(ImportMetadataStructureTaskManager.FILENAME, fileName);
                TaskManager.AddToBus(ImportMetadataStructureTaskManager.EXTENTION, "." + fileName.Split('.').Last());
                Session["TaskManager"] = TaskManager;
            }

            //return RedirectToAction("UploadWizard");
            return Content("");
        }


        /// <summary>
        /// read filenames from datapath/Temp/Username
        /// </summary>
        /// <returns>return a list with all names from FileStream in the folder</returns>
        private List<String> GetServerFileList()
        {

            string userDataPath = Path.Combine(AppConfiguration.DataPath, "Temp", GetUserNameOrDefault());

            // if folder not exist
            if (!Directory.Exists(userDataPath))
            {
                Directory.CreateDirectory(userDataPath);
            }


            DirectoryInfo dirInfo = new DirectoryInfo(userDataPath);
            return dirInfo.GetFiles().Select(i => i.Name).ToList();

        }

        // chekc if user exist
        // if true return usernamem otherwise "DEFAULT"
        public string GetUserNameOrDefault()
        {
            string userName = string.Empty;
            try
            {
                userName = HttpContext.User.Identity.Name;
            }
            catch { }

            return !string.IsNullOrWhiteSpace(userName) ? userName : "DEFAULT";
        }

        /// <summary>
        /// returns true if Extention in the Bus will supported
        /// </summary>
        /// <param name="taskManager"></param>
        /// <returns></returns>
        private bool IsSupportedExtention(ImportMetadataStructureTaskManager taskManager)
        {
            if (taskManager.Bus.ContainsKey(ImportMetadataStructureTaskManager.EXTENTION))
            {
                string ext = taskManager.Bus[ImportMetadataStructureTaskManager.EXTENTION].ToString();
                if (ImportMetadataStructureWizardHelper.GetExtentionList().Contains(ext.ToLower())) return true;
            }

            return false;
        }

        #endregion
    }
}