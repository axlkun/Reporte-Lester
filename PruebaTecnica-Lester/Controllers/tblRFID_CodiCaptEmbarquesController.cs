using OfficeOpenXml;
using PruebaTecnica_Lester.Models;
using System;
using System.Collections.Generic;
using System.Data.Common.CommandTrees.ExpressionBuilder;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace PruebaTecnica_Lester.Controllers
{
    public class tblRFID_CodiCaptEmbarquesController : Controller
    {
        // GET: tblRFID_CodiCaptEmbarques
        public ActionResult Index(DateTime? startDate, DateTime? endDate, string reportType)
        {
           
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            ViewBag.ReportType = reportType;

                if ((startDate == null && endDate == null) || (startDate == null) || (startDate > endDate))
                {
                    return View();
                }
                else
                {
                    if (reportType == "Unitario")
                    {
                        var data = ReporteUnitario(startDate, endDate);
                        ViewBag.ReportData = data;
                        return View();
                    }
                    else
                    {
                        var data = ReporteSecuencial(startDate, endDate);
                        ViewBag.ReportData = data;
                        return View();
                    }

                }

        }

        private List<tblRFID_CodiCaptEmbarques> ReporteUnitario(DateTime? startDate, DateTime? endDate)
        {
            using (DbModels context = new DbModels())
            {
                IQueryable<tblRFID_CodiCaptEmbarques> query = context.tblRFID_CodiCaptEmbarques;

                if (startDate != null && endDate != null)
                {
                    endDate = endDate.Value.AddDays(1);
                    query = query.Where(x => x.fechaLectura >= startDate && x.fechaLectura <= endDate);
                }
                else if (startDate != null && endDate == null)
                {
                    endDate = startDate.Value.AddDays(1);
                    query = query.Where(x => x.fechaLectura >= startDate && x.fechaLectura <= endDate);
                }

                return query.ToList();
            }
        }

        private List<ReporteSecuencial> ReporteSecuencial(DateTime? startDate, DateTime? endDate)
        {
            using (DbModels context = new DbModels())
            {
                if (startDate != null && endDate != null)
                {
                    endDate = endDate.Value.AddDays(1);

                    var query = context.tblRFID_CodiCaptEmbarques
                    .Where(x => x.fechaLectura >= startDate && x.fechaLectura <= endDate)
                    .GroupBy(x => new { x.Viaje, x.acronimo })
                    .Select(g => new ReporteSecuencial
                    {
                        Secuencia = g.Key.Viaje,
                        Acronimo = g.Key.acronimo,
                        Cantidad = g.Count()
                    })
                    .OrderBy(x => x.Secuencia)
                    .ThenByDescending(x => x.Cantidad)
                    .ToList();

                    return query;
                }
                else
                {
                    endDate = startDate.Value.AddDays(1);

                    var query = context.tblRFID_CodiCaptEmbarques
                    .Where(x => x.fechaLectura >= startDate && x.fechaLectura <= endDate)
                    .GroupBy(x => new { x.Viaje, x.acronimo })
                    .Select(g => new ReporteSecuencial
                    {
                        Secuencia = g.Key.Viaje,
                        Acronimo = g.Key.acronimo,
                        Cantidad = g.Count()
                    })
                    .OrderBy(x => x.Secuencia)
                    .ThenByDescending(x => x.Cantidad)
                    .ToList();

                    return query;
                }

            }
        }

        public ActionResult ExcelUnitario(string reportData)
        {
            var serializer = new JavaScriptSerializer();
            List<tblRFID_CodiCaptEmbarques> data = serializer.Deserialize<List<tblRFID_CodiCaptEmbarques>>(reportData);

            byte[] fileContents;

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("ReportData");

                worksheet.Cells["A1"].Value = "RFID";
                worksheet.Cells["B1"].Value = "Acrónimo";
                worksheet.Cells["C1"].Value = "Hora de lectura";

                int row = 2;
                foreach (var item in data)
                {
                    worksheet.Cells["A" + row].Value = item.codebar;
                    worksheet.Cells["B" + row].Value = item.acronimo;

                    DateTime fechaLocal = item.fechaLectura != null ? item.fechaLectura.Value.ToLocalTime() : DateTime.MinValue;
                    worksheet.Cells["C" + row].Value = fechaLocal.ToString("dd/MM/yyyy hh:mm:ss tt"); // Convertir la fecha a cadena en el formato deseado
                    row++;
                }

                fileContents = package.GetAsByteArray();

            }

            return File(fileContents, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ReporteUnitario.xlsx");
        }

        public ActionResult ExcelSecuencial(string reportData)
        {
            var serializer = new JavaScriptSerializer();
            List<ReporteSecuencial> data = serializer.Deserialize<List<ReporteSecuencial>>(reportData);

            byte[] fileContents;

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("ReportData");

                worksheet.Cells["A1"].Value = "Secuencia";
                worksheet.Cells["B1"].Value = "Acrónimo";
                worksheet.Cells["C1"].Value = "Cantidad";

                int row = 2;
                foreach (var item in data)
                {
                    worksheet.Cells["A" + row].Value = item.Secuencia;
                    worksheet.Cells["B" + row].Value = item.Acronimo;
                    worksheet.Cells["C" + row].Value = item.Cantidad;
                    row++;
                }

                fileContents = package.GetAsByteArray();

            }

            return File(fileContents, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ReporteSecuencial.xlsx");
        }

    }
}
