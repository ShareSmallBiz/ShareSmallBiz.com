namespace ApiTestGenerator.Models
{
    public class PathItem
    {
        public OperationObject Get { get; set; }
        public OperationObject Post { get; set; }
        public OperationObject Put { get; set; }
        public OperationObject Delete { get; set; }
        public OperationObject Patch { get; set; }
    }
}
