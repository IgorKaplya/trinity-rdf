﻿// LICENSE:
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
// AUTHORS:
//
//  Moritz Eberl <moritz@semiodesk.com>
//  Sebastian Faubel <sebastian@semiodesk.com>
//
// Copyright (c) Semiodesk GmbH 2015

using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Clauses.ResultOperators;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using VDS.RDF.Query;
using VDS.RDF.Query.Aggregates.Sparql;
using VDS.RDF.Query.Builder;
using VDS.RDF.Query.Expressions.Primary;

// TODO:
// - Support scalar query forms.
// - Establish dynamic look-up of query source reference expressions and sub query expresssions to variable names.
// - Make selected variables of sub queries more flexible (currently only subject and object).

namespace Semiodesk.Trinity.Query
{
    internal class QueryModelVisitor : QueryModelVisitorBase
    {
        #region Members

        private readonly ExpressionTreeVisitor _expressionVisitor;

        private readonly QueryGeneratorTree _queryGeneratorTree;

        private readonly QueryGenerator _rootGenerator;

        private readonly Dictionary<QueryModel, QueryGenerator> _queryGenerators = new Dictionary<QueryModel, QueryGenerator>();

        public QueryModel CurrentQueryModel { get; private set; }

        public QueryGenerator CurrentQueryGenerator { get; private set; }

        public VariableBuilder VariableBuilder { get; private set; }

        #endregion

        #region Constructors

        public QueryModelVisitor()
        {
            VariableBuilder = new VariableBuilder();

            // The root query which selects triples when returning resources.
            _rootGenerator = new QueryGenerator(this);

            CurrentQueryGenerator = _rootGenerator;

            // Add the root query builder to the query tree.
            _queryGeneratorTree = new QueryGeneratorTree(_rootGenerator);

            // The expression tree visitor needs to be initialized *after* the query builders.
            _expressionVisitor = new ExpressionTreeVisitor(this);
        }

        #endregion

        #region Methods

        public override void VisitAdditionalFromClause(AdditionalFromClause fromClause, QueryModel queryModel, int index)
        {
            throw new NotSupportedException();
        }

        public override void VisitGroupJoinClause(GroupJoinClause groupJoinClause, QueryModel queryModel, int index)
        {
            throw new NotSupportedException();
        }

        public override void VisitJoinClause(JoinClause joinClause, QueryModel queryModel, int index)
        {
            throw new NotSupportedException();
        }

        public override void VisitJoinClause(JoinClause joinClause, QueryModel queryModel, GroupJoinClause groupJoinClause)
        {
            throw new NotSupportedException();
        }

        public override void VisitMainFromClause(MainFromClause fromClause, QueryModel queryModel)
        {
            if (fromClause.FromExpression.NodeType == ExpressionType.MemberAccess)
            {
                MemberExpression memberExpression = fromClause.FromExpression as MemberExpression;

                if(memberExpression.Expression is SubQueryExpression)
                {
                    VisitSubQueryExpression(memberExpression.Expression as SubQueryExpression);
                }

                Debug.WriteLine(fromClause.GetType().ToString() + ": " + fromClause.ItemName);

                _expressionVisitor.VisitExpression(fromClause.FromExpression);
            }
            else
            {
                Debug.WriteLine(fromClause.GetType().ToString() + ": " + fromClause.ItemName);

                string s = fromClause.ItemName;

                _rootGenerator.SetSubject(new SparqlVariable(s));
                _rootGenerator.Select(new SparqlVariable("p_"));
                _rootGenerator.SetObject(new SparqlVariable("o_"));

                _rootGenerator.Where(e => e.Subject(s).Predicate("p_").Object("o_"));
            }
        }

        public override void VisitQueryModel(QueryModel queryModel)
        {
            Debug.WriteLine(queryModel.GetType().ToString());

            QueryModel currentQueryModel = CurrentQueryModel;

            CurrentQueryModel = queryModel;

            queryModel.SelectClause.Accept(this, queryModel);

            CurrentQueryModel = currentQueryModel;
        }

        public override void VisitResultOperator(ResultOperatorBase resultOperator, QueryModel queryModel, int index)
        {
            Debug.WriteLine(resultOperator.GetType().ToString());

            QueryGenerator generator = GetCurrentQueryGenerator();

            if (resultOperator is AnyResultOperator)
            {
                var aggregate = new SampleAggregate(new VariableTerm(generator.ObjectVariable.Name));
                generator.SetObject(aggregate.AsSparqlVariable());
            }
            else if (resultOperator is AverageResultOperator)
            {
                var aggregate = new AverageAggregate(new VariableTerm(generator.ObjectVariable.Name));
                generator.SetObject(aggregate.AsSparqlVariable());
            }
            else if(resultOperator is CountResultOperator)
            {
                var aggregate = new CountAggregate(new VariableTerm(generator.ObjectVariable.Name));
                generator.SetObject(aggregate.AsSparqlVariable());
            }
            else if(resultOperator is FirstResultOperator)
            {
                var aggregate = new MinAggregate(new VariableTerm(generator.ObjectVariable.Name));
                generator.SetObject(aggregate.AsSparqlVariable());
                generator.OrderBy(generator.ObjectVariable);
            }
            else if(resultOperator is LastResultOperator)
            {
                var aggregate = new MinAggregate(new VariableTerm(generator.ObjectVariable.Name));
                generator.SetObject(aggregate.AsSparqlVariable());
                generator.OrderByDescending(generator.ObjectVariable);
            }
            else if(resultOperator is MaxResultOperator)
            {
                var aggregate = new MaxAggregate(new VariableTerm(generator.ObjectVariable.Name));
                generator.SetObject(aggregate.AsSparqlVariable());
            }
            else if(resultOperator is MinResultOperator)
            {
                var aggregate = new MinAggregate(new VariableTerm(generator.ObjectVariable.Name));
                generator.SetObject(aggregate.AsSparqlVariable());
            }
            else if(resultOperator is SumResultOperator)
            {
                var aggregate = new SumAggregate(new VariableTerm(generator.ObjectVariable.Name));
                generator.SetObject(aggregate.AsSparqlVariable());
            }
            else if (resultOperator is OfTypeResultOperator)
            {
                Type itemType = queryModel.MainFromClause.ItemType;
                RdfClassAttribute itemClass = itemType.TryGetCustomAttribute<RdfClassAttribute>();

                if (itemClass == null)
                {
                    throw new ArgumentException("No RdfClass attrribute declared on type: " + itemType);
                }

                SparqlVariable s = generator.SubjectVariable;
                Uri o = itemClass.MappedUri;

                generator.Where(e => e.Subject(s.Name).PredicateUri("rdf:type").Object(o));
            }
            else if(resultOperator is SkipResultOperator)
            {
                SkipResultOperator op = resultOperator as SkipResultOperator;
                generator.Offset(int.Parse(op.Count.ToString()));
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public override void VisitSelectClause(SelectClause selectClause, QueryModel queryModel)
        {
            Debug.WriteLine(selectClause.GetType().ToString());

            queryModel.MainFromClause.Accept(this, queryModel);

            for(int i = 0; i < queryModel.BodyClauses.Count; i++)
            {
                IBodyClause c = queryModel.BodyClauses[i];

                c.Accept(this, queryModel, i);
            }

            for(int i = 0; i < queryModel.ResultOperators.Count; i++)
            {
                ResultOperatorBase o = queryModel.ResultOperators[i];

                o.Accept(this, queryModel, i);
            }
        }

        public override void VisitWhereClause(WhereClause whereClause, QueryModel queryModel, int index)
        {
            if (whereClause.Predicate is BinaryExpression)
            {
                BinaryExpression binaryExpression = whereClause.Predicate as BinaryExpression;

                if(binaryExpression.Left is SubQueryExpression)
                {
                    VisitSubQueryExpression(binaryExpression.Left as SubQueryExpression);
                }

                if(binaryExpression.Right is SubQueryExpression)
                {
                    VisitSubQueryExpression(binaryExpression.Right as SubQueryExpression);
                }

                Debug.WriteLine(whereClause.GetType().ToString());
            }
            else
            {
                Debug.WriteLine(whereClause.GetType().ToString());
            }

            _expressionVisitor.VisitExpression(whereClause.Predicate);
        }

        private void VisitSubQueryExpression(SubQueryExpression expression)
        {
            QueryGenerator currentQueryGenerator = CurrentQueryGenerator;
            QueryGenerator subQueryGenerator = new QueryGenerator(this);

            // Enable look-up of query generators from query models.
            _queryGenerators[expression.QueryModel] = subQueryGenerator;

            // Add the sub query to the query tree.
            _queryGeneratorTree.AddQuery(currentQueryGenerator, subQueryGenerator);

            CurrentQueryGenerator = subQueryGenerator;

            _expressionVisitor.VisitExpression(expression);

            CurrentQueryGenerator = currentQueryGenerator;
        }

        public override void VisitOrderByClause(OrderByClause orderByClause, QueryModel queryModel, int index)
        {
            Debug.WriteLine(orderByClause.GetType().ToString());

            base.VisitOrderByClause(orderByClause, queryModel, index);
        }

        public override void VisitOrdering(Ordering ordering, QueryModel queryModel, OrderByClause orderByClause, int index)
        {
            Debug.WriteLine(ordering.GetType().ToString());

            base.VisitOrdering(ordering, queryModel, orderByClause, index);
        }

        public ISparqlQuery GetQuery()
        {
            string queryString = "";

            // Since the dotNetRdf QueryBuilder does not support building sub queries,
            // we need to generate the nested queries here.
            _queryGeneratorTree.Traverse((builder) =>
            {
                string q = builder.BuildQuery().ToString();

                if(!string.IsNullOrEmpty(queryString))
                {
                    int n = q.IndexOf("{") + 1;

                    if(n > 0)
                    {
                        q = q.Insert(n, "{ " + queryString + " }");
                    }
                }

                queryString = q;
            });

            ISparqlQuery query = new SparqlQuery(queryString);

            Debug.WriteLine(query.ToString());

            return query;
        }

        public QueryGenerator GetCurrentQueryGenerator()
        {
            return CurrentQueryGenerator;
        }

        public QueryGenerator GetQueryGenerator(QueryModel queryModel)
        {
            return _queryGenerators[queryModel];
        }

        #endregion
    }
}