using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication2 {
	class Program {
		static List<Foo> query = new List<Foo>();
		static int N = 50;
		static Random rand = new Random(1000);
		const int FOO_NO = 500000;
		const int BAR_NO = 100;
		const int MAX_BAR_PER_FOO = 10;
		const int ITERATION_NO = 1000;
		static void Main(string[] args) {
			for (int i = 0; i < FOO_NO; ++i)
				query.Add(new Foo() { Id = i, Bars = new List<Bar>() });
			List<Bar> bars = new List<Bar>();
			for (int i = 0; i < BAR_NO; ++i)
				bars.Add(new Bar() { Id = i });
			foreach (Foo foo in query) {
				List<int> barNos = Enumerable.Range(0, BAR_NO).ToList();
				int numberOfBar = rand.Next(1, MAX_BAR_PER_FOO - 1);
				for (int i = 0; i < numberOfBar; ++i) {
					int index = rand.Next(0, barNos.Count);
					foo.Bars.Add(bars[barNos[index]]);
					barNos.RemoveAt(index);
				}
			}

			DateTime start;
			TimeSpan ts;
			string[] names = { "ian", "spender_dupehandle", "rob", "davidb", "ivanstoev_1" };
			MethodDelegate[] methods = { method_ian, method_spender, method_rob, method_davidb, method_ivanstoev_1 };

			for (int k = 0; k < names.Length; ++k) {
				start = DateTime.Now;
				for (int i = 0; i < ITERATION_NO; ++i)
					methods[k]();
				ts = DateTime.Now - start;
				Console.WriteLine(names[k] + ": " + ts.TotalMilliseconds + " ms");
			}

			Console.ReadKey();
		}

		public class Foo {
			public int Id { get; set; }
			public ICollection<Bar> Bars { get; set; }
		}

		public class Bar {
			public int Id { get; set; }
		}

		private delegate void MethodDelegate();

		public class FooSimilarityComparer_spender : IEqualityComparer<Foo> {
			public bool Equals(Foo a, Foo b) {
				//called infrequently
				return a.Bars.OrderBy(bar => bar.Id).SequenceEqual(b.Bars.OrderBy(bar => bar.Id));
			}
			public int GetHashCode(Foo foo) {
				//called frequently
				unchecked {
					return foo.Bars.Sum(b => b.GetHashCode());
				}
			}
		}

		private static void method_spender() {
			var hs = new HashSet<Foo>(new FooSimilarityComparer_spender());
			foreach (var f in query) {
				hs.Add(f); //hashsets don't add duplicates, as measured by the FooSimilarityComparer
				if (hs.Count >= N) {
					break;
				}
			}
		}

		private static bool areBarsSimilar_ian(ICollection<Bar> bars1, ICollection<Bar> bars2) {
			return bars1.Count == bars2.Count && //have the same amount of bars
					bars1.Select(x => x.Id)
					.Except(bars2.Select(y => y.Id))
					.ToList().Count == 0; //and when excepted returns 0, mean similar bar
		}

		private static void method_ian() {
			List<Foo> topNFoos = new List<Foo>(); //this serves as a memory for the previous query
			foreach (var q in query) { //query is IOrderedEnumerable or IEnumerable
				if (topNFoos.Count == 0 || !topNFoos.Any(foo => areBarsSimilar_ian(foo.Bars, q.Bars)))
					topNFoos.Add(q);
				if (topNFoos.Count >= N) //We have had enough Foo
					break;
			}
		}

		private static void method_rob() {
			var res = query.Select(q => new {
				original = q,
				matches = query.Where(innerQ => areBarsSimilar_ian(q.Bars, innerQ.Bars))
			}).Select(g => new { original = g, joinKey = string.Join(",", g.matches.Select(m => m.Id)) })
			.GroupBy(g => g.joinKey)
			.Select(g => g.First().original.original)
			.Take(N);
		}

		private static void method_ivanstoev_1() {
			var result = query.Aggregate(new List<Foo>(), (list, next) => {
				if (list.Count < N && !list.Any(item => areBarsSimilar_ian(item.Bars, next.Bars)))
					list.Add(next);
				return list;
			});
		}

		private static void method_davidb() {
			IEnumerable<Foo> dissimilarFoos =
				from foo in query
				let key = string.Join("|",
					from bar in foo.Bars
					orderby bar.Id
					select bar.Id.ToString())
				group foo by key into g
				select g.First();
			IEnumerable<Foo> firstDissimilarFoos = dissimilarFoos.Take(N);
		}
	}
}
