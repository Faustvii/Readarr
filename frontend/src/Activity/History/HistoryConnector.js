import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import withCurrentPage from 'Components/withCurrentPage';
import { fetchBooksByIds } from 'Store/Actions/bookActions';
import * as historyActions from 'Store/Actions/historyActions';
import { registerPagePopulator, unregisterPagePopulator } from 'Utilities/pagePopulator';
import History from './History';

function createMapStateToProps() {
  return createSelector(
    (state) => state.history,
    (state) => state.authors,
    (state) => state.books,
    (history, authors, books) => {
      return {
        isAuthorFetching: authors.isFetching,
        isAuthorPopulated: authors.isPopulated,
        isBooksFetching: books.isFetching,
        isBooksPopulated: books.isPopulated,
        booksError: books.error,
        books: books.items,
        ...history
      };
    }
  );
}

const mapDispatchToProps = {
  ...historyActions,
  fetchBooksByIds
};

class HistoryConnector extends Component {

  constructor(props) {
    super(props);
    this.state = {
      fetchedBookIds: new Set()
    };
  }

  //
  // Lifecycle

  componentDidMount() {
    const {
      useCurrentPage,
      fetchHistory,
      gotoHistoryFirstPage
    } = this.props;

    registerPagePopulator(this.repopulate);

    if (useCurrentPage) {
      fetchHistory();
    } else {
      gotoHistoryFirstPage();
    }
  }

  componentDidUpdate(prevProps) {
    if (this.props.items && this.props.items.length > 0 && !this.props.isBooksFetching) {
      const bookIds = this.props.items
        .map((item) => item.bookId)
        .filter((bookId) => bookId != null)
        .filter((bookId, index, array) => array.indexOf(bookId) === index);
      const currentBooks = this.props.books || [];
      const missingBookIds = bookIds.filter((bookId) =>
        !currentBooks.some((book) => book.id === bookId)
      );
      const newBookIds = missingBookIds.filter((bookId) => !this.state.fetchedBookIds.has(bookId));
      if (newBookIds.length > 0) {
        this.setState((prevState) => ({
          fetchedBookIds: new Set([...prevState.fetchedBookIds, ...newBookIds])
        }));
        this.props.fetchBooksByIds({ bookIds: newBookIds });
      }
    }
  }

  componentWillUnmount() {
    unregisterPagePopulator(this.repopulate);
    this.props.clearHistory();
  }

  //
  // Control

  repopulate = () => {
    this.props.fetchHistory();
  };

  //
  // Listeners

  onFirstPagePress = () => {
    this.props.gotoHistoryFirstPage();
  };

  onPreviousPagePress = () => {
    this.props.gotoHistoryPreviousPage();
  };

  onNextPagePress = () => {
    this.props.gotoHistoryNextPage();
  };

  onLastPagePress = () => {
    this.props.gotoHistoryLastPage();
  };

  onPageSelect = (page) => {
    this.props.gotoHistoryPage({ page });
  };

  onSortPress = (sortKey) => {
    this.props.setHistorySort({ sortKey });
  };

  onFilterSelect = (selectedFilterKey) => {
    this.props.setHistoryFilter({ selectedFilterKey });
  };

  onTableOptionChange = (payload) => {
    this.props.setHistoryTableOption(payload);

    if (payload.pageSize) {
      this.props.gotoHistoryFirstPage();
    }
  };

  //
  // Render

  render() {
    return (
      <History
        onFirstPagePress={this.onFirstPagePress}
        onPreviousPagePress={this.onPreviousPagePress}
        onNextPagePress={this.onNextPagePress}
        onLastPagePress={this.onLastPagePress}
        onPageSelect={this.onPageSelect}
        onSortPress={this.onSortPress}
        onFilterSelect={this.onFilterSelect}
        onTableOptionChange={this.onTableOptionChange}
        {...this.props}
      />
    );
  }
}

HistoryConnector.propTypes = {
  useCurrentPage: PropTypes.bool.isRequired,
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  books: PropTypes.arrayOf(PropTypes.object).isRequired,
  isBooksPopulated: PropTypes.bool.isRequired,
  isBooksFetching: PropTypes.bool.isRequired,
  fetchHistory: PropTypes.func.isRequired,
  fetchBooksByIds: PropTypes.func.isRequired,
  gotoHistoryFirstPage: PropTypes.func.isRequired,
  gotoHistoryPreviousPage: PropTypes.func.isRequired,
  gotoHistoryNextPage: PropTypes.func.isRequired,
  gotoHistoryLastPage: PropTypes.func.isRequired,
  gotoHistoryPage: PropTypes.func.isRequired,
  setHistorySort: PropTypes.func.isRequired,
  setHistoryFilter: PropTypes.func.isRequired,
  setHistoryTableOption: PropTypes.func.isRequired,
  clearHistory: PropTypes.func.isRequired
};

export default withCurrentPage(
  connect(createMapStateToProps, mapDispatchToProps)(HistoryConnector)
);
