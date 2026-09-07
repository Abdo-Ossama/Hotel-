namespace Hotel_Booking.DTOs.Response
{
    public class PagedResponse<T>
    {
        public IEnumerable<T> Items { get; set; } = [];

        public int CurrentPage { get; set; }
        public int HasPrevious { get; set; }
        public int HasNext { get; set; }

        public int PageSize { get; set; }

 
        public int TotalCount { get; set; }

        public int TotalPages { get; set; }
    }
}