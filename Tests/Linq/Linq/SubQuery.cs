﻿using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class SubQuery : TestBase
	{
		[Test]
		public void Test1([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					where p.ParentID != 5
					select (from ch in Child where ch.ParentID == p.ParentID select ch.ChildID).Max(),
					from p in db.Parent
					where p.ParentID != 5
					select (from ch in db.Child where ch.ParentID == p.ParentID select ch.ChildID).Max());
		}

		[Test]
		public void Test2([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					where p.ParentID != 5
					select (from ch in Child where ch.ParentID == p.ParentID && ch.ChildID > 1 select ch.ChildID).Max(),
					from p in db.Parent
					where p.ParentID != 5
					select (from ch in db.Child where ch.ParentID == p.ParentID && ch.ChildID > 1 select ch.ChildID).Max());
		}

		[Test]
		public void Test3([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					where p.ParentID != 5
					select (from ch in Child where ch.ParentID == p.ParentID && ch.ChildID == ch.ParentID * 10 + 1 select ch.ChildID).SingleOrDefault()
					,
					from p in db.Parent
					where p.ParentID != 5
					select (from ch in db.Child where ch.ParentID == p.ParentID && ch.ChildID == ch.ParentID * 10 + 1 select ch.ChildID).SingleOrDefault());
		}

		[Test]
		public void Test4([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					where p.ParentID != 5
					select (from ch in Child where ch.ParentID == p.ParentID && ch.ChildID == ch.ParentID * 10 + 1 select ch.ChildID).FirstOrDefault(),
					from p in db.Parent
					where p.ParentID != 5
					select (from ch in db.Child where ch.ParentID == p.ParentID && ch.ChildID == ch.ParentID * 10 + 1 select ch.ChildID).FirstOrDefault());
		}

		static int _testValue = 3;

		[Test]
		public void Test5([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
			{
				IEnumerable<int> ids = new[] { 1, 2 };

				var eids = Parent
					.Where(p => ids.Contains(p.ParentID))
					.Select(p => p.Value1 == null ? p.ParentID : p.ParentID + 1)
					.Distinct();

				var expected = eids.Select(id =>
					new 
					{
						id,
						Count1 = Child.Where(p => p.ParentID == id).Count(),
						Count2 = Child.Where(p => p.ParentID == id && p.ParentID == _testValue).Count(),
					});

				var rids   = db.Parent
					.Where(p => ids.Contains(p.ParentID))
					.Select(p => p.Value1 == null ? p.ParentID : p.ParentID + 1)
					.Distinct();

				var result = rids.Select(id =>
					new
					{
						id,
						Count1 = db.Child.Where(p => p.ParentID == id).Count(),
						Count2 = db.Child.Where(p => p.ParentID == id && p.ParentID == _testValue).Count(),
					});

				AreEqual(expected, result);
			}
		}

		[Test]
		public void Test6([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
			{
				var id = 2;
				var b  = false;

				var q = Child.Where(c => c.ParentID == id).OrderBy(c => c.ChildID);
				q = b
					? q.OrderBy(m => m.ParentID)
					: q.OrderByDescending(m => m.ParentID);

				var gc = GrandChild;
				var expected = q.Select(c => new
				{
					ID     = c.ChildID,
					c.ParentID,
					Sum    = gc.Where(g => g.ChildID == c.ChildID && g.GrandChildID > 0).Sum(g => (int)g.ChildID * g.GrandChildID),
					Count1 = gc.Count(g => g.ChildID == c.ChildID && g.GrandChildID > 0)
				});

				var r = db.Child.Where(c => c.ParentID == id).OrderBy(c => c.ChildID);
				r = b
					? r.OrderBy(m => m.ParentID)
					: r.OrderByDescending(m => m.ParentID);

				var rgc = db.GrandChild;
				var result = r.Select(c => new
				{
					ID     = c.ChildID,
					c.ParentID,
					Sum    = rgc.Where(g => g.ChildID == c.ChildID && g.GrandChildID > 0).Sum(g => (int)g.ChildID * g.GrandChildID),
					Count1 = rgc.Count(g => g.ChildID == c.ChildID && g.GrandChildID > 0),
				});

				AreEqual(expected, result);
			}
		}

		[Test]
		public void Test7([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from c in    Child select new { Count =    GrandChild.Where(g => g.ChildID == c.ChildID).Count() },
					from c in db.Child select new { Count = db.GrandChild.Where(g => g.ChildID == c.ChildID).Count() });
		}

		[Test]
		public void Test8([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
			{
				var parent  =
					from p in db.Parent
					where p.ParentID == 1
					select p.ParentID;

				var chilren =
					from c in db.Child
					where parent.Contains(c.ParentID)
					select c;

				var chs1 = chilren.ToList();

				parent  =
					from p in db.Parent
					where p.ParentID == 2
					select p.ParentID;

				chilren =
					from c in db.Child
					where parent.Contains(c.ParentID)
					select c;

				var chs2 = chilren.ToList();

				Assert.AreEqual(chs2.Count, chs2.Except(chs1).Count());
			}
		}

		[Test]
		public void ObjectCompare([DataContexts(ProviderName.Access)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					from c in
						from c in
							from c in Child select new Child { ParentID = c.ParentID, ChildID = c.ChildID + 1, Parent = c.Parent }
						where c.ChildID > 0
						select c
					where p == c.Parent
					select new { p.ParentID, c.ChildID },
					from p in db.Parent
					from c in
						from c in
							from c in db.Child select new Child { ParentID = c.ParentID, ChildID = c.ChildID + 1, Parent = c.Parent }
						where c.ChildID > 0
						select c
					where p == c.Parent
					select new { p.ParentID, c.ChildID });
		}

		[Test]
		public void Contains1([DataContexts(ProviderName.Informix, ProviderName.MySql, ProviderName.Sybase)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					where (from p1 in    Parent where p1.Value1 == p.Value1 select p.ParentID).Take(3).Contains(p.ParentID)
					select p,
					from p in db.Parent
					where (from p1 in db.Parent where p1.Value1 == p.Value1 select p.ParentID).Take(3).Contains(p.ParentID)
					select p);
		}

		[Test]
		public void Contains2([DataContexts(ProviderName.Informix, ProviderName.MySql, ProviderName.Sybase)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					where (from p1 in    Parent where p1.Value1 == p.Value1 select p1.ParentID).Take(3).Contains(p.ParentID)
					select p,
					from p in db.Parent
					where (from p1 in db.Parent where p1.Value1 == p.Value1 select p1.ParentID).Take(3).Contains(p.ParentID)
					select p);
		}

		[Test]
		public void SubSub1([DataContexts(
			ProviderName.SqlCe, ProviderName.Access, ProviderName.DB2, ProviderName.Oracle, ProviderName.MySql, ProviderName.Sybase)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p1 in
						from p2 in Parent
						select new { p2, ID = p2.ParentID + 1 }
					where p1.ID > 0
					select new
					{
						Count =
						(
							from c in p1.p2.Children
							select new { c, ID = c.ParentID + 1 } into c
							where c.ID < p1.ID
							select c
						).Count()
					},
					from p1 in
						from p2 in db.Parent
						select new { p2, ID = p2.ParentID + 1 }
					where p1.ID > 0
					select new
					{
						Count =
						(
							from c in p1.p2.Children
							select new { c, ID = c.ParentID + 1 } into c
							where c.ID < p1.ID
							select c
						).Count()
					});
		}

		[Test]
		public void SubSub2([DataContexts(
			ProviderName.Access, ProviderName.DB2, ProviderName.Oracle, ProviderName.MySql, ProviderName.Sybase, ProviderName.Informix)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p1 in
						from p2 in Parent
						select new { p2, ID = p2.ParentID + 1 } into p3
						where p3.ID > 0
						select new { p2 = p3, ID = p3.ID + 1 }
					where p1.ID > 0
					select new
					{
						Count =
						(
							from c in p1.p2.p2.Children
							select new { c, ID = c.ParentID + 1 } into c
							where c.ID < p1.ID
							select c.c.ParentID + 1 into c
							where c < p1.ID
							select c
						).FirstOrDefault()
					},
					from p1 in
						from p2 in db.Parent
						select new { p2, ID = p2.ParentID + 1 } into p3
						where p3.ID > 0
						select new { p2 = p3, ID = p3.ID + 1 }
					where p1.ID > 0
					select new
					{
						Count =
						(
							from c in p1.p2.p2.Children
							select new { c, ID = c.ParentID + 1 } into c
							where c.ID < p1.ID
							select c.c.ParentID + 1 into c
							where c < p1.ID
							select c
						).FirstOrDefault()
					});
		}

		//[Test]
		public void SubSub201([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p1 in
						from p2 in Parent
						select new { p2, ID = p2.ParentID + 1 } into p3
						where p3.ID > 0
						select new { p2 = p3, ID = p3.ID + 1 }
					where p1.ID > 0
					select new
					{
						Count =
						(
							from c in p1.p2.p2.Children
							select new { c, ID = c.ParentID + 1 } into c
							where c.ID < p1.ID
							select new { c.c, ID = c.c.ParentID + 1 } into c
							where c.ID < p1.ID
							select c
						).FirstOrDefault()
					},
					from p1 in
						from p2 in db.Parent
						select new { p2, ID = p2.ParentID + 1 } into p3
						where p3.ID > 0
						select new { p2 = p3, ID = p3.ID + 1 }
					where p1.ID > 0
					select new
					{
						Count =
						(
							from c in p1.p2.p2.Children
							select new { c, ID = c.ParentID + 1 } into c
							where c.ID < p1.ID
							select new { c.c, ID = c.c.ParentID + 1 } into c
							where c.ID < p1.ID
							select c
						).FirstOrDefault()
					});
		}

		[Test]
		public void SubSub21([DataContexts(
			ProviderName.SqlCe, ProviderName.DB2, ProviderName.Oracle, ProviderName.MySql, ProviderName.Sybase, ProviderName.Access)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p1 in
						from p2 in Parent
						select new { p2, ID = p2.ParentID + 1 } into p3
						where p3.ID > 0
						select new { p2 = p3, ID = p3.ID + 1 }
					where p1.ID > 0
					select new
					{
						Count =
						(
							from c in p1.p2.p2.Children
							select new { c, ID = c.ParentID + 1 } into c
							where c.ID < p1.ID
							select new { c.c, ID = c.c.ParentID + 1 } into c
							where c.ID < p1.ID
							select c
						).Count()
					},
					from p1 in
						from p2 in db.Parent
						select new { p2, ID = p2.ParentID + 1 } into p3
						where p3.ID > 0
						select new { p2 = p3, ID = p3.ID + 1 }
					where p1.ID > 0
					select new
					{
						Count =
						(
							from c in p1.p2.p2.Children
							select new { c, ID = c.ParentID + 1 } into c
							where c.ID < p1.ID
							select new { c.c, ID = c.c.ParentID + 1 } into c
							where c.ID < p1.ID
							select c
						).Count()
					});
		}

		[Test]
		public void SubSub211([DataContexts(
			ProviderName.SqlCe, ProviderName.Access, ProviderName.DB2, ProviderName.Oracle, ProviderName.MySql, ProviderName.Sybase)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p1 in
						from p2 in Parent
						select new { p2, ID = p2.ParentID + 1 } into p3
						where p3.ID > 0
						select new { p2 = p3, ID = p3.ID + 1 }
					where p1.ID > 0
					select new
					{
						Count =
						(
							from c in p1.p2.p2.Children
							from g in c.GrandChildren
							select new { g, ID = g.ParentID + 1 } into c
							where c.ID < p1.ID
							select new { c.g, ID = c.g.ParentID + 1 } into c
							where c.ID < p1.ID
							select c
						).Count()
					},
					from p1 in
						from p2 in db.Parent
						select new { p2, ID = p2.ParentID + 1 } into p3
						where p3.ID > 0
						select new { p2 = p3, ID = p3.ID + 1 }
					where p1.ID > 0
					select new
					{
						Count =
						(
							from c in p1.p2.p2.Children
							from g in c.GrandChildren
							select new { g, ID = g.ParentID + 1 } into c
							where c.ID < p1.ID
							select new { c.g, ID = c.g.ParentID + 1 } into c
							where c.ID < p1.ID
							select c
						).Count()
					});
		}

		[Test]
		public void SubSub212([DataContexts(
			ProviderName.SqlCe, ProviderName.Access, ProviderName.DB2, ProviderName.Oracle, ProviderName.MySql, ProviderName.Sybase)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p1 in
						from p2 in Child
						select new { p2, ID = p2.ParentID + 1 } into p3
						where p3.ID > 0
						select new { p2 = p3, ID = p3.ID + 1 }
					where p1.ID > 0
					select new
					{
						Count =
						(
							from c in p1.p2.p2.Parent.GrandChildren
							select new { c, ID = c.ParentID + 1 } into c
							where c.ID < p1.ID
							select new { c.c, ID = c.c.ParentID + 1 } into c
							where c.ID < p1.ID
							select c
						).Count()
					},
					from p1 in
						from p2 in db.Child
						select new { p2, ID = p2.ParentID + 1 } into p3
						where p3.ID > 0
						select new { p2 = p3, ID = p3.ID + 1 }
					where p1.ID > 0
					select new
					{
						Count =
						(
							from c in p1.p2.p2.Parent.GrandChildren
							select new { c, ID = c.ParentID + 1 } into c
							where c.ID < p1.ID
							select new { c.c, ID = c.c.ParentID + 1 } into c
							where c.ID < p1.ID
							select c
						).Count()
					});
		}

		[Test]
		public void SubSub22([DataContexts(
			ProviderName.SqlCe, ProviderName.Access, ProviderName.DB2, ProviderName.Oracle, ProviderName.MySql, ProviderName.Sybase)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p1 in
						from p2 in Parent
						select new { p2, ID = p2.ParentID + 1 } into p3
						where p3.ID > 0
						select new { p2 = p3, ID = p3.ID + 1 }
					where p1.ID > 0
					select new
					{
						Count =
						(
							from c in Child
							where p1.p2.p2.ParentID == c.ParentID
							select new { c, ID = c.ParentID + 1 } into c
							where c.ID < p1.ID
							select new { c.c, ID = c.c.ParentID + 1 } into c
							where c.ID < p1.ID
							select c
						).Count()
					},
					from p1 in
						from p2 in db.Parent
						select new { p2, ID = p2.ParentID + 1 } into p3
						where p3.ID > 0
						select new { p2 = p3, ID = p3.ID + 1 }
					where p1.ID > 0
					select new
					{
						Count =
						(
							from c in db.Child
							where p1.p2.p2.ParentID == c.ParentID
							select new { c, ID = c.ParentID + 1 } into c
							where c.ID < p1.ID
							select new { c.c, ID = c.c.ParentID + 1 } into c
							where c.ID < p1.ID
							select c
						).Count()
					});
		}

		[Test]
		public void LetTest1([DataContexts(ProviderName.SqlCe, ProviderName.Informix, ProviderName.Sybase)] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from p in Parent
					let ch = p.Children
					where ch.FirstOrDefault() != null
					select ch.FirstOrDefault().ParentID
					,
					from p in db.Parent
					let ch = p.Children
					where ch.FirstOrDefault() != null
					select ch.FirstOrDefault().ParentID);
			}
		}

		[Test]
		public void LetTest2([DataContexts(ProviderName.SqlCe, ProviderName.Informix, ProviderName.Sybase)] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from p in Parent
					let ch = p.Children
					where ch.FirstOrDefault() != null
					select p
					,
					from p in db.Parent
					let ch = p.Children
					where ch.FirstOrDefault() != null
					select p);
			}
		}

		[Test]
		public void LetTest3([DataContexts(ProviderName.Informix, ProviderName.Sybase)] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from p in Parent
					let ch = Child
					select ch.FirstOrDefault().ParentID
					,
					from p in db.Parent
					let ch = db.Child
					select ch.FirstOrDefault().ParentID);
			}
		}

		[Test]
		public void LetTest4([DataContexts(ProviderName.Informix, ProviderName.Sybase)] string context)
		{
			LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = true;

			using (var db = GetDataContext(context))
			{
				AreEqual(
					from p in Parent
					let ch1 = Child.Where(c => c.ParentID == p.ParentID)
					let ch2 = ch1.Where(c => c.ChildID > -100)
					select new
					{
						Any    = ch2.Any(),
						Count  = ch2.Count(),
						First1 = ch2.FirstOrDefault(c => c.ParentID > 0),
						First2 = ch2.FirstOrDefault()
					}
					,
					from p in db.Parent
					let ch1 = db.Child.Where(c => c.ParentID == p.ParentID)
					let ch2 = ch1.Where(c => c.ChildID > -100)
					select new
					{
						Any    = ch2.Any(),
						Count  = ch2.Count(),
						First1 = ch2.FirstOrDefault(c => c.ParentID > 0),
						First2 = ch2.FirstOrDefault()
					});
			}

			LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = false;
		}

		[Test]
		public void LetTest5([DataContexts(ProviderName.Informix, ProviderName.Sybase)] string context)
		{
			LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = true;

			using (var db = GetDataContext(context))
			{
				AreEqual(
					from p in Parent
					let ch1 = Child.Where(c => c.ParentID == p.ParentID)
					let ch2 = ch1.Where(c => c.ChildID > -100)
					select new
					{
						Any    = ch2.Any(),
						Count  = ch2.Count(),
						First1 = ch2.FirstOrDefault(c => c.ParentID > 0) == null ? 0 : ch2.FirstOrDefault(c => c.ParentID > 0).ParentID,
						First2 = ch2.FirstOrDefault()
					}
					,
					from p in db.Parent
					let ch1 = db.Child.Where(c => c.ParentID == p.ParentID)
					let ch2 = ch1.Where(c => c.ChildID > -100)
					select new
					{
						Any    = ch2.Any(),
						Count  = ch2.Count(),
						First1 = ch2.FirstOrDefault(c => c.ParentID > 0).ParentID,
						First2 = ch2.FirstOrDefault()
					});
			}

			LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = false;
		}

		[Test]
		public void LetTest6([DataContexts(ProviderName.Informix, ProviderName.Sybase)] string context)
		{
			LinqToDB.Common.Configuration.Linq.AllowMultipleQuery     = true;
			//LinqToDB.Common.Configuration.Linq.GenerateExpressionTest = true;

			using (var db = GetDataContext(context))
				AreEqual(
					(
						from p in Parent
						let ch1 = Child.Where(c => c.ParentID == p.ParentID)
						let ch2 = ch1.Where(c => c.ChildID > -100)
						select new
						{
							p.ParentID,
							Any    = ch2.Any(),
							Count  = ch2.Count(),
							First1 = ch2.FirstOrDefault(c => c.ParentID > 0) == null ? 0 : ch2.FirstOrDefault(c => c.ParentID > 0).ParentID,
							First2 = ch2.FirstOrDefault()
						}
					).Where(t => t.ParentID > 0)
					,
					(
						from p in db.Parent
						let ch1 = db.Child.Where(c => c.ParentID == p.ParentID)
						let ch2 = ch1.Where(c => c.ChildID > -100)
						select new
						{
							p.ParentID,
							Any    = ch2.Any(),
							Count  = ch2.Count(),
							First1 = ch2.FirstOrDefault(c => c.ParentID > 0).ParentID,
							First2 = ch2.FirstOrDefault()
						}
					).Where(t => t.ParentID > 0));

			LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = false;
		}

		[Test]
		public void LetTest7([DataContexts(ProviderName.Informix, ProviderName.Sybase)] string context)
		{
			LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = true;

			using (var db = GetDataContext(context))
				AreEqual(
					(
						from p in Parent
						let ch1 = Child.Where(c => c.ParentID == p.ParentID)
						let ch2 = ch1.Where(c => c.ChildID > -100)
						select new
						{
							p.ParentID,
							Any    = ch2.Any(),
							Count  = ch2.Count(),
							First1 = ch2.FirstOrDefault(c => c.ParentID > 0) == null ? 0 : ch2.FirstOrDefault(c => c.ParentID > 0).ParentID,
							First2 = ch2.FirstOrDefault()
						}
					).Where(t => t.ParentID > 0).Take(5000)
					,
					(
						from p in db.Parent
						let ch1 = db.Child.Where(c => c.ParentID == p.ParentID)
						let ch2 = ch1.Where(c => c.ChildID > -100)
						select new
						{
							p.ParentID,
							Any    = ch2.Any(),
							Count  = ch2.Count(),
							First1 = ch2.FirstOrDefault(c => c.ParentID > 0).ParentID,
							First2 = ch2.FirstOrDefault()
						}
					).Where(t => t.ParentID > 0).Take(5000));

			LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = false;
		}

		[Test]
		public void LetTest8([DataContexts] string context)
		{
			LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = true;

			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					let ch1 = Child.Where(c => c.ParentID == p.ParentID)
					let ch2 = ch1.Where(c => c.ChildID > -100)
					let ch3	= ch2.FirstOrDefault(c => c.ParentID > 0)
					select new
					{
						Any    = ch2.Any(),
						Count  = ch2.Count(),
						First1 = ch3 == null ? 0 : ch3.ParentID,
						First2 = ch2.FirstOrDefault()
					}
					,
					from p in db.Parent
					let ch1 = db.Child.Where(c => c.ParentID == p.ParentID)
					let ch2 = ch1.Where(c => c.ChildID > -100)
					let ch3	= ch2.FirstOrDefault(c => c.ParentID > 0)
					select new
					{
						Any    = ch2.Any(),
						Count  = ch2.Count(),
						First1 = ch3 == null ? 0 : ch3.ParentID,
						First2 = ch2.FirstOrDefault()
					});

			LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = false;
		}

		[Test]
		public void LetTest9([DataContexts] string context)
		{
			LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = true;

			using (var db = GetDataContext(context))
				AreEqual(
					(
						from p in Parent
						let ch1 = Child.Where(c => c.ParentID == p.ParentID)
						let ch2 = ch1.Where(c => c.ChildID > -100)
						select new
						{
							First = ch2.FirstOrDefault()
						}
					).Take(10)
					,
					(
						from p in db.Parent
						let ch1 = db.Child.Where(c => c.ParentID == p.ParentID)
						let ch2 = ch1.Where(c => c.ChildID > -100)
						select new
						{
							First = ch2.FirstOrDefault()
						}
					).Take(10));

			LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = false;
		}
	}
}
