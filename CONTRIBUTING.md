# Development Team
financeiroti.cobranca@stone.com.br

# How to contribute?
* Use git-flow and the concepts of features and hotfixes. 
* Open Pull Requests and ask for code reviews before merging to the main branches (develop and master)

# On Events and Aggregates namespaces changes and the impact on existing data stored
* We are using a SQL Server event store implementation
* This implementation uses the assembly classified namespace of Events to save and retrieve them from the store
* At this current version, you are not allowed to change existing aggregates and events namespaces or class names
* That is so because we are using JSON parsing and string compression, so the data already persisted would be corrupted.
 
