namespace Application.DataAccessLayer.Service.Common
{
    public class PagingHelpers
    {
        public int TotalItems { get; private set; }
        public int CurrentPage { get; private set; }
        public int PageSize { get; private set; }
        public int TotalPages { get; private set; }
        public int StartPage { get; private set; }
        public int EndPage { get; private set; }
        public string? NamePage { get; set; }

        private const int PagesAroundCurrent = 5;

        private PagingHelpers() { }

        public static PagingHelpers Create(int totalItems, int page, int pageSize = 8, int pagesAroundCurrent = 5)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 1;

            int totalPages = (int)Math.Ceiling((decimal)totalItems / pageSize);
            int currentPage = Math.Min(page, totalPages);

            int startPage = currentPage - pagesAroundCurrent;
            int endPage = currentPage + pagesAroundCurrent;

            if (startPage <= 0)
            {
                endPage -= (startPage - 1);
                startPage = 1;
            }

            if (endPage > totalPages)
            {
                endPage = totalPages;
                if (endPage > 2 * pagesAroundCurrent + 1)
                {
                    startPage = endPage - 2 * pagesAroundCurrent;
                }
            }

            return new PagingHelpers
            {
                TotalItems = totalItems,
                CurrentPage = currentPage,
                PageSize = pageSize,
                TotalPages = totalPages,
                StartPage = startPage,
                EndPage = endPage
            };
        }
    }
}
