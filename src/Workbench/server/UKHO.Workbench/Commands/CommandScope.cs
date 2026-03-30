namespace UKHO.Workbench.Commands
{
    /// <summary>
    /// Identifies whether a Workbench command belongs to the host shell or to a hosted tool.
    /// </summary>
    public enum CommandScope
    {
        /// <summary>
        /// Represents a command owned by the Workbench host shell.
        /// </summary>
        Host,

        /// <summary>
        /// Represents a command owned by a hosted tool instance.
        /// </summary>
        Tool
    }
}
