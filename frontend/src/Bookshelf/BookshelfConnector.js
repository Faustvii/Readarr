import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { fetchBooksByAuthor } from 'Store/Actions/bookActions';
import { saveBookshelf, setBookshelfFilter, setBookshelfSort } from 'Store/Actions/bookshelfActions';
import createAuthorClientSideCollectionItemsSelector from 'Store/Selectors/createAuthorClientSideCollectionItemsSelector';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import { registerPagePopulator, unregisterPagePopulator } from 'Utilities/pagePopulator';
import Bookshelf from './Bookshelf';

function createBookFetchStateSelector() {
  return createSelector(
    (state) => state.books,
    (booksState) => {
      const bookCount = (!booksState.isFetching && booksState.isPopulated) ? booksState.items.length : 0;
      return {
        bookCount,
        isFetching: booksState.isFetching,
        isPopulated: booksState.isPopulated,
        page: booksState.page,
        totalPages: booksState.totalPages,
        totalRecords: booksState.totalRecords,
        pageSize: booksState.pageSize
      };
    }
  );
}

function createMapStateToProps() {
  return createSelector(
    createBookFetchStateSelector(),
    createAuthorClientSideCollectionItemsSelector('bookshelf'),
    createDimensionsSelector(),
    (books, author, dimensionsState) => {
      const isPopulated = books.isPopulated && author.isPopulated;
      const isFetching = author.isFetching || books.isFetching;
      return {
        ...author,
        isPopulated,
        isFetching,
        bookCount: books.bookCount,
        isBooksPopulated: books.isPopulated,
        isSmallScreen: dimensionsState.isSmallScreen
      };
    }
  );
}

const mapDispatchToProps = {
  setBookshelfSort,
  setBookshelfFilter,
  saveBookshelf,
  fetchBooksByAuthor
};

class BookshelfConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    registerPagePopulator(this.populate);
    this.populate();
  }

  componentDidUpdate(prevProps) {
    const prevAuthorIds = prevProps.items ? prevProps.items.map((a) => a.id).sort().join(',') : '';
    const currentAuthorIds = this.props.items ? this.props.items.map((a) => a.id).sort().join(',') : '';

    if (prevAuthorIds !== currentAuthorIds && this.props.items) {
      this.props.items.forEach((author) => {
        const statistics = author.statistics;
        const hasCompleteBookList = statistics && statistics.totalBookCount !== 0 && author.books.length >= statistics.totalBookCount;

        if (!hasCompleteBookList) {
          this.props.fetchBooksByAuthor({ authorId: author.id });
        }
      });
    }
  }

  componentWillUnmount() {
    unregisterPagePopulator(this.populate);
  }

  //
  // Control

  populate = () => {
    const { items } = this.props;

    if (items && items.length > 0) {
      items.forEach((author) => {
        const statistics = author.statistics;
        const hasCompleteBookList = statistics && statistics.totalBookCount !== 0 && author.books.length >= statistics.totalBookCount;

        if (!hasCompleteBookList) {
          this.props.fetchBooksByAuthor({ authorId: author.id });
        }
      });
    }
  };

  //
  // Listeners

  onSortPress = (sortKey) => {
    this.props.setBookshelfSort({ sortKey });
  };

  onFilterSelect = (selectedFilterKey) => {
    this.props.setBookshelfFilter({ selectedFilterKey });
  };

  onUpdateSelectedPress = (payload) => {
    this.props.saveBookshelf(payload);
  };

  //
  // Render

  render() {
    return (
      <Bookshelf
        {...this.props}
        onSortPress={this.onSortPress}
        onFilterSelect={this.onFilterSelect}
        onUpdateSelectedPress={this.onUpdateSelectedPress}
      />
    );
  }
}

BookshelfConnector.propTypes = {
  isSmallScreen: PropTypes.bool.isRequired,
  setBookshelfSort: PropTypes.func.isRequired,
  setBookshelfFilter: PropTypes.func.isRequired,
  saveBookshelf: PropTypes.func.isRequired,
  fetchBooksByAuthor: PropTypes.func.isRequired,
  items: PropTypes.arrayOf(PropTypes.object).isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(BookshelfConnector);
