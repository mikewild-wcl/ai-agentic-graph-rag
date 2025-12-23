#FROM neo4j:2025.09.0
FROM neo4j:latest

ENV NEO4J_ACCEPT_LICENSE_AGREEMENT=yes

# Ask Neo4j to install APOC and Graph Data Science on startup
ENV NEO4J_PLUGINS='["apoc","apoc-extended","graph-data-science"]'

# Allow APOC and GDS procedures to run without procedure-level blocking and set import dir
ENV NEO4J_dbms_security_procedures_unrestricted=apoc.*,gds.*
ENV NEO4J_dbms_directories_import=/import
ENV apoc.import.file.enabled=true
ENV apoc.import.file.use_neo4j_config=false

# Expose HTTP and Bolt
EXPOSE 7474 7687

# Persist data/logs/plugins/import across container restarts
VOLUME ["/data", "/logs", "/import", "/plugins"]
