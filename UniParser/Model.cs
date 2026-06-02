using System;
using System.ComponentModel.DataAnnotations;

namespace UniParser;
public class TrackedProduct
{
	[Key]
	public int Id { get; set; }
	public string Title { get; set; }
	public string Url { get; set; }
	public int FixedPrice { get; set; }
	public DateTime UpdatedAt { get; set; }
}
public class UserMonitored
{
	[Key]
	public int Id { get; set; }
	public long ChatId { get; set; }
	public int TrackedProductId { get; set; }
}
