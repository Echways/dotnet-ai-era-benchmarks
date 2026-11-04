import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))

import classify
import methods
import prs
from common import CCA_ACCOUNT_ID, Library, coauthor_ids

COMMIT = {"author_email": "someone@example.com"}


def record(**overrides):
    base = {"status": "ok", "author_id": 1, "author_login": "dev", "author_type": "User", "author_association": "CONTRIBUTOR"}
    return {**base, **overrides}


def test_agent_pull_request_is_classified_cca_by_account_id():
    assert classify.author_class(record(author_id=CCA_ACCOUNT_ID, author_type="Bot", author_login="Copilot"), COMMIT) == "cca"


def test_member_is_msft_human_and_contributor_is_community():
    assert classify.author_class(record(author_association="MEMBER"), COMMIT) == "msft-human"
    assert classify.author_class(record(author_association="CONTRIBUTOR"), COMMIT) == "community"


def test_other_bots_are_not_agent_work():
    assert classify.author_class(record(author_type="Bot", author_login="dotnet-maestro"), COMMIT) == "bot"


def test_commit_without_pull_request_is_no_pr_unless_authored_by_the_agent():
    assert classify.author_class({"status": "no-pr"}, COMMIT) == "no-pr"
    agent = {"author_email": classify.CCA_EMAIL}
    assert classify.author_class({"status": "no-pr"}, agent) == "cca"


def test_unresolved_commit_is_pending():
    assert classify.author_class({"status": "pending"}, COMMIT) == "pending"


def test_coauthor_trailer_with_noreply_address_yields_the_account_id():
    message = "Fix\n\nCo-authored-by: Copilot <198982749+Copilot@users.noreply.github.com>\nCo-authored-by: A <a@example.com>"
    assert coauthor_ids(message) == [198982749]


def test_pick_prefers_the_pull_request_whose_merge_commit_is_the_commit():
    pulls = [{"number": 1, "mergedAt": "x", "mergeCommit": {"oid": "other"}}, {"number": 2, "mergedAt": None, "mergeCommit": {"oid": "abc"}}]
    assert prs.pick(pulls, "abc")["number"] == 2


def test_pick_falls_back_to_a_merged_pull_request():
    pulls = [{"number": 1, "mergedAt": None, "mergeCommit": None}, {"number": 2, "mergedAt": "x", "mergeCommit": None}]
    assert prs.pick(pulls, "abc")["number"] == 2


def test_describe_marks_commits_missing_from_the_store_as_pending():
    library = Library("P", "runtime", ("src/p",))
    assert prs.describe({}, library, {"sha": "abc", "subject": "s"}) == {"sha": "abc", "status": "pending"}


def test_describe_maps_a_graphql_pull_request_to_a_record():
    pull = {
        "number": 7,
        "title": "t",
        "url": "u",
        "authorAssociation": "MEMBER",
        "createdAt": "c",
        "mergedAt": "m",
        "baseRefName": "main",
        "mergeCommit": {"oid": "abc"},
        "additions": 3,
        "deletions": 1,
        "changedFiles": 2,
        "labels": {"nodes": [{"name": "b"}, {"name": "a"}]},
        "author": {"__typename": "Bot", "login": "Copilot", "databaseId": CCA_ACCOUNT_ID},
    }
    result = prs.describe({"abc": [pull]}, Library("P", "runtime", ("src/p",)), {"sha": "abc", "subject": "s"})
    assert result["author_type"] == "Bot"
    assert result["author_id"] == CCA_ACCOUNT_ID
    assert result["labels"] == ["a", "b"]


def test_describe_survives_a_deleted_author():
    pull = {
        "number": 7,
        "title": "t",
        "url": "u",
        "authorAssociation": "NONE",
        "createdAt": "c",
        "mergedAt": "m",
        "baseRefName": "main",
        "mergeCommit": {"oid": "abc"},
        "additions": 0,
        "deletions": 0,
        "changedFiles": 0,
        "labels": {"nodes": []},
        "author": None,
    }
    result = prs.describe({"abc": [pull]}, Library("P", "runtime", ("src/p",)), {"sha": "abc", "subject": "s"})
    assert result["author_login"] == "ghost"


def test_shared_sources_map_to_the_common_directory_of_the_repository():
    library = Library("P", "runtime", ("src/libraries/P/src",))
    assert methods.repository_path(library, "Common/Extensions/X.cs") == "src/libraries/Common/src/Extensions/X.cs"
    assert methods.repository_path(library, "A/B.cs") == "src/libraries/P/src/A/B.cs"
