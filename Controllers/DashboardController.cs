using ExpenseTracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Syncfusion.EJ2.PdfViewer;

namespace ExpenseTracker.Controllers
{
    public class DashboardController : Controller
    {
        public readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            DateTime StartDate = DateTime.Today.AddDays(-14);
            DateTime EndDate = DateTime.Today;

            List<Transaction> SelectedTransations = await _context.Transactions.Include(x => x.Category).Where(y=>y.Date >= StartDate && y.Date <= EndDate).ToListAsync();

            int TotalIncome = SelectedTransations.Where(i => i.Category.Type == "Income").Sum(j => j.Amount);

            ViewBag.TotalIncome = TotalIncome.ToString("C0");

            int TotalExpense = SelectedTransations.Where(i => i.Category.Type == "Expense").Sum(j => j.Amount);

            ViewBag.TotalExpense = TotalExpense.ToString("C0");


            int Balance = TotalIncome - TotalExpense;
            ViewBag.Balance = Balance.ToString("C0");


            ViewBag.DonutChartData = SelectedTransations.Where(i => i.Category.Type == "Expense").GroupBy(j => j.Category.CategoryId)
                .Select(k => new
                {
                    categoryTitleWithIcon = k.First().Category.Icon + " " + k.First().Category.Title,
                    amount = k.Sum(j => j.Amount),
                    formattedAmount = k.Sum(j => j.Amount).ToString("C0"),
                })
                .OrderByDescending(l=>l.amount)
                .ToList();

            List<SplineChartData> IncomeSummmary = SelectedTransations.Where(i => i.Category.Type is "Income").GroupBy(j => j.Date)
                .Select(k => new SplineChartData()
                {
                    day = k.First().Date.ToString("dd-MMM"),
                    income = k.Sum(l => l.Amount)

                }).ToList();

            List<SplineChartData> ExpenseSummary = SelectedTransations.Where(i => i.Category.Type is "Expense").GroupBy(j => j.Date)
               .Select(k => new SplineChartData()
               {
                   day = k.First().Date.ToString("dd-MMM"),
                   expense = k.Sum(l => l.Amount)

               }).ToList();


            string[] last14Days = Enumerable.Range(0, 14).Select(i => StartDate.AddDays(i).ToString("dd-MMM")).ToArray();

            ViewBag.SplineChartData = from day in last14Days join income in IncomeSummmary on day equals income.day
                                      into dayIncomeJoined from income in dayIncomeJoined.DefaultIfEmpty()
                                      join expense in ExpenseSummary on day equals expense.day into expenseJoined
                                      from expense in expenseJoined.DefaultIfEmpty()
                                      select new
                                      {
                                          day = day,
                                          income = income is null ? 0 : income.income,
                                          expense = expense is null ? 0 : expense.expense,
                                      };


            ViewBag.RecentTransactions = await _context.Transactions.Include(i => i.Category).OrderByDescending(j => j.Date).Take(5).ToListAsync();

            return View();
        }
    }

    public class SplineChartData
    {
        public string day;
        public int income;
        public int expense;
    }
}
