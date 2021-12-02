/*
 * Copyright (C) 2021 - 2021, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you 
 * may not use this file except in compliance with the License. You may 
 * obtain a copy of the License at 
 * 
 * http://www.apache.org/licenses/LICENSE-2.0 
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
 * License for the specific language governing permissions and limitations under 
 * the License.
 * 
 * User: fyfej
 * Date: 2021-8-5
 */
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using SanteDB.Core.Model;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Services;

namespace SanteDB.BusinessRules.JavaScript.Test
{
    [ExcludeFromCodeCoverage]
    internal class ConceptRepository : IRepositoryService<Concept>, IRepositoryService
    {
        public string ServiceName => "Dummy Concept Repo";

        public IEnumerable<Concept> Find(Expression<Func<Concept, bool>> query)
        {
            return this.Find(query, 0, null, out int t);
        }

        public IEnumerable<Concept> Find(Expression<Func<Concept, bool>> query, int offset, int? count, out int totalResults, params ModelSort<Concept>[] orderBy)
        {
            totalResults = 1;
            return new List<Concept>() {
                new Concept()
                {
                    Mnemonic = "Stuff!!!"
                }
            };
        }

        public IEnumerable<IdentifiedData> Find(Expression query)
        {
            return this.Find((Expression<Func<Concept, bool>>)query);

        }

        public IEnumerable<IdentifiedData> Find(Expression query, int offset, int? count, out int totalResults)
        {
            return this.Find((Expression<Func<Concept, bool>>)query, offset, count, out totalResults);
        }

        public Concept Get(Guid key)
        {
            return new Concept()
            {
                Mnemonic = "Stuff!!!"
            };
        }

        public Concept Get(Guid key, Guid versionKey)
        {
            return new Concept()
            {
                Mnemonic = "Stuff!!!"
            };
        }

        public Concept Insert(Concept data)
        {
            return new Concept()
            {
                Mnemonic = "Stuff!!!"
            };
        }

        public IdentifiedData Insert(object data)
        {
            throw new NotImplementedException();
        }

        public Concept Obsolete(Guid key)
        {
            return new Concept()
            {
                Mnemonic = "Stuff!!!"
            };
        }

        public Concept Save(Concept data)
        {
            return new Concept()
            {
                Mnemonic = "Stuff!!!"
            };
        }

        public IdentifiedData Save(object data)
        {
            throw new NotImplementedException();
        }

        IdentifiedData IRepositoryService.Get(Guid key)
        {
            throw new NotImplementedException();
        }

        IdentifiedData IRepositoryService.Obsolete(Guid key)
        {
            throw new NotImplementedException();
        }
    }
}