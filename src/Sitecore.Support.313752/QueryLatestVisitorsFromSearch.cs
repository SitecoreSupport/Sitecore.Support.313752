namespace Sitecore.Cintel.Reporting.Aggregate.Visitors.Processors
{
  using System;
  using System.Data;
  using System.Linq;
  using Sitecore.Analytics.Model;
  using Sitecore.Cintel.Configuration;
  using Sitecore.Cintel.Reporting.Processors;
  using Sitecore.Cintel.Reporting.Utility;
  using Sitecore.Common;
  using Sitecore.ContentSearch;
  using Sitecore.ContentSearch.Analytics.Models;
  using Sitecore.ContentSearch.Linq.Extensions;
  using Sitecore.ContentSearch.Linq.Nodes;
  using Sitecore.ContentSearch.Utilities;
  public class QueryLatestVisitorsFromSearch : ReportProcessorBase
  {
    public override void Process(ReportProcessorArgs args)
    {
      var analyticsIndex = ContentSearchManager.GetIndex(CustomerIntelligenceConfig.ContactSearch.SearchIndexName);

      using (var ctx = analyticsIndex.CreateSearchContext())
      {
        var rawSearchResults = ctx.GetQueryable<IndexedContact>();

        args.ResultSet.TotalResultCount = ctx.GetQueryable<IndexedContact>().Count();

        rawSearchResults.ForEach(sr =>
          {
            var row = args.ResultTableForView.NewRow();
            BuildBaseResult(row, sr);
            args.ResultTableForView.Rows.Add(row);

            var latestVisit = ctx.GetQueryable<IndexedVisit>()
                            .Where(iv => iv.ContactId == sr.ContactId)
                            .OrderByDescending(iv => iv.StartDateTime)
                                    .Take(1)
                                    .FirstOrDefault();

            if (null != latestVisit)
            {
              PopulateLatestVisit(latestVisit, ref row);
            }
          });
      }
    }

    #region private methods

    private void BuildBaseResult(DataRow row, IndexedContact ic)
    {
      ContactIdentificationLevel ident;
      if (!Enum.TryParse(ic.IdentificationLevel, true, out ident))
      {
        ident = ContactIdentificationLevel.None;
      }

      row[Schema.ContactIdentificationLevel.Name] = (int)ident;
      row[Schema.ContactId.Name] = ic.ContactId;
      row[Schema.FirstName.Name] = ic.FirstName;
      row[Schema.MiddleName.Name] = ic.MiddleName;
      row[Schema.Surname.Name] = ic.Surname;
      row[Schema.EmailAddress.Name] = ic.PreferredEmail;  //TODO: this needs logic
      row[Schema.Value.Name] = ic.Value;
      row[Schema.VisitCount.Name] = ic.VisitCount;
      row[Schema.ValuePerVisit.Name] = Calculator.GetAverageValue(ic.Value, ic.VisitCount);
    }

    private void PopulateLatestVisit(IndexedVisit visit, ref DataRow row)
    {
      row[Schema.LatestVisitValue.Name] = visit.Value;
      row[Schema.LatestVisitStartDateTime.Name] = visit.StartDateTime;
      row[Schema.LatestVisitEndDateTime.Name] = visit.EndDateTime;
      row[Schema.LatestVisitDuration.Name] = Calculator.GetDuration(visit.StartDateTime, visit.EndDateTime);

      if (null != visit.WhoIs)
      {
        row[Schema.LatestVisitCityDisplayName.Name] = visit.WhoIs.City;
        row[Schema.LatestVisitCountryDisplayName.Name] = visit.WhoIs.Country;
        row[Schema.LatestVisitRegionDisplayName.Name] = visit.WhoIs.Region;
      }
    }

    #endregion

  }


}
